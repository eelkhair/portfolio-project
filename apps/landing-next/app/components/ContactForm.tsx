"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";

type Status = "idle" | "sending" | "success" | "error";

declare global {
  interface Window {
    turnstile?: {
      render: (container: HTMLElement, options: Record<string, unknown>) => string;
      reset: (widgetId: string) => void;
      remove: (widgetId: string) => void;
    };
  }
}

export function ContactForm() {
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [subject, setSubject] = useState("");
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [errorMsg, setErrorMsg] = useState("");
  const [turnstileToken, setTurnstileToken] = useState("");
  const turnstileRef = useRef<HTMLDivElement>(null);
  const widgetIdRef = useRef<string | null>(null);

  const renderWidget = useCallback(() => {
    if (!turnstileRef.current || !window.turnstile) return;
    if (widgetIdRef.current !== null) {
      window.turnstile.remove(widgetIdRef.current);
    }
    widgetIdRef.current = window.turnstile.render(turnstileRef.current, {
      sitekey: process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY,
      theme: "dark",
      callback: (token: string) => setTurnstileToken(token),
      "expired-callback": () => setTurnstileToken(""),
      "error-callback": () => setTurnstileToken(""),
    });
  }, []);

  useEffect(() => {
    if (window.turnstile) {
      renderWidget();
      return;
    }

    const script = document.createElement("script");
    script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
    script.async = true;
    script.onload = () => renderWidget();
    document.head.appendChild(script);

    return () => {
      if (widgetIdRef.current !== null && window.turnstile) {
        window.turnstile.remove(widgetIdRef.current);
      }
    };
  }, [renderWidget]);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();

    if (!turnstileToken) {
      console.warn("[landing] contact submit blocked: missing turnstile token");
      setStatus("error");
      setErrorMsg("Please complete the verification.");
      return;
    }

    console.info("[landing] contact submit start", { hasEmail: !!email, subjectLen: subject.length, messageLen: message.length });
    setStatus("sending");
    setErrorMsg("");

    try {
      const res = await fetch("/api/contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ name, email, subject, message, token: turnstileToken }),
      });

      const data = await res.json();

      if (!res.ok) {
        console.error("[landing] contact submit failed", { status: res.status, error: data.error });
        setStatus("error");
        setErrorMsg(data.error || "Something went wrong.");
        if (widgetIdRef.current !== null && window.turnstile) {
          window.turnstile.reset(widgetIdRef.current);
          setTurnstileToken("");
        }
        return;
      }

      console.info("[landing] contact submit success", { id: data.id });
      setStatus("success");
      setName("");
      setEmail("");
      setSubject("");
      setMessage("");
      setTurnstileToken("");
    } catch (err) {
      console.error("[landing] contact submit network error", { err: String(err) });
      setStatus("error");
      setErrorMsg("Network error. Please try again.");
    }
  }

  if (status === "success") {
    return (
      <div className="contact-success">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="var(--green)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
          <polyline points="22 4 12 14.01 9 11.01" />
        </svg>
        <h2>Message sent!</h2>
        <p>Thanks for reaching out. I&apos;ll get back to you soon.</p>
        <button className="btn btn-primary" onClick={() => { setStatus("idle"); setTimeout(renderWidget, 100); }}>
          Send another message
        </button>
      </div>
    );
  }

  return (
    <form className="contact-form" onSubmit={handleSubmit}>
      <div className="form-group">
        <label htmlFor="name">Name *</label>
        <input
          id="name"
          type="text"
          value={name}
          onChange={(e) => setName(e.target.value)}
          required
          placeholder="Your name"
        />
      </div>

      <div className="form-group">
        <label htmlFor="email">Email *</label>
        <input
          id="email"
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          placeholder="you@example.com"
        />
      </div>

      <div className="form-group">
        <label htmlFor="subject">Subject</label>
        <input
          id="subject"
          type="text"
          value={subject}
          onChange={(e) => setSubject(e.target.value)}
          placeholder="What is this about?"
        />
      </div>

      <div className="form-group">
        <label htmlFor="message">Message *</label>
        <textarea
          id="message"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          required
          rows={6}
          placeholder="Your message..."
        />
      </div>

      <div ref={turnstileRef} />

      {status === "error" && <p className="form-error">{errorMsg}</p>}

      <button
        type="submit"
        className="btn btn-primary contact-submit"
        disabled={status === "sending"}
      >
        {status === "sending" ? "Sending..." : "Send Message"}
      </button>
    </form>
  );
}
