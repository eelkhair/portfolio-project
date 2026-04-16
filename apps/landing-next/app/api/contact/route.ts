import { NextRequest, NextResponse } from "next/server";
import nodemailer from "nodemailer";

const rateLimit = new Map<string, number>();
const RATE_LIMIT_MS = 60_000;

export async function POST(req: NextRequest) {
  const ip = req.headers.get("x-forwarded-for") ?? "unknown";
  const now = Date.now();
  const lastSent = rateLimit.get(ip);
  if (lastSent && now - lastSent < RATE_LIMIT_MS) {
    console.warn(`[Contact] Rate limited: ip=${ip}`);
    return NextResponse.json(
      { error: "Please wait before sending another message." },
      { status: 429 },
    );
  }

  let body: { name?: string; email?: string; subject?: string; message?: string; token?: string };
  try {
    body = await req.json();
  } catch (err) {
    console.error("[Contact] Failed to parse request body:", err);
    return NextResponse.json({ error: "Invalid request." }, { status: 400 });
  }

  const { name, email, subject, message, token } = body;

  if (!token) {
    console.error("[Contact] Missing Turnstile token: ip=%s", ip);
    return NextResponse.json({ error: "Verification required." }, { status: 400 });
  }

  const turnstileRes = await fetch("https://challenges.cloudflare.com/turnstile/v0/siteverify", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      secret: process.env.TURNSTILE_SECRET_KEY,
      response: token,
      remoteip: ip,
    }),
  });
  const turnstileData = await turnstileRes.json();
  if (!turnstileData.success) {
    console.error("[Contact] Turnstile verification failed: ip=%s, errors=%j", ip, turnstileData["error-codes"]);
    return NextResponse.json({ error: "Verification failed. Please try again." }, { status: 403 });
  }

  if (!name?.trim() || !email?.trim() || !message?.trim()) {
    return NextResponse.json(
      { error: "Name, email, and message are required." },
      { status: 400 },
    );
  }

  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  if (!emailRegex.test(email)) {
    return NextResponse.json({ error: "Invalid email address." }, { status: 400 });
  }

  const transporter = nodemailer.createTransport({
    host: process.env.SMTP_HOST,
    port: Number(process.env.SMTP_PORT),
    secure: false,
    auth: {
      user: process.env.SMTP_USER,
      pass: process.env.SMTP_PASS,
    },
  });

  try {
    await transporter.sendMail({
      from: process.env.SMTP_USER,
      to: process.env.CONTACT_TO,
      replyTo: email,
      subject: subject?.trim() || `Portfolio Contact: ${name}`,
      text: `Name: ${name}\nEmail: ${email}\n\n${message}`,
      html: `
        <h3>New Contact Form Submission</h3>
        <p><strong>Name:</strong> ${escapeHtml(name)}</p>
        <p><strong>Email:</strong> ${escapeHtml(email)}</p>
        <p><strong>Subject:</strong> ${escapeHtml(subject?.trim() || "N/A")}</p>
        <hr />
        <p>${escapeHtml(message).replace(/\n/g, "<br />")}</p>
      `,
    });

    rateLimit.set(ip, now);
    console.log("[Contact] Email sent: from=%s, subject=%s", email, subject?.trim() || `Portfolio Contact: ${name}`);
    return NextResponse.json({ success: true });
  } catch (err) {
    console.error("[Contact] SMTP error: from=%s, to=%s, error=%s", email, process.env.CONTACT_TO, err);
    return NextResponse.json(
      { error: "Failed to send message. Please try again later." },
      { status: 500 },
    );
  }
}

function escapeHtml(str: string): string {
  return str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;");
}
