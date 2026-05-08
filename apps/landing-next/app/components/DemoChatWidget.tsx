"use client";

import { FormEvent, useCallback, useEffect, useRef, useState } from "react";
import { useFeatureFlags } from "./FeatureFlags";
import { generateRandomString, pkceChallenge } from "../lib/pkce";

type Status = "idle" | "sending" | "creating" | "created" | "error";

interface ChatMessage {
  role: "user" | "assistant";
  text: string;
}

interface CreatedCompany {
  uid: string;
  name: string;
  expiresAt: string;
  claimToken: string;
  traceId: string;
}

declare global {
  interface Window {
    turnstile?: {
      render: (container: HTMLElement, options: Record<string, unknown>) => string;
      reset: (widgetId: string) => void;
      remove: (widgetId: string) => void;
    };
  }
}

const STORAGE_KEY = "lastDemoCompany";

export function DemoChatWidget() {
  const flags = useFeatureFlags();
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState("");
  const [status, setStatus] = useState<Status>("idle");
  const [errorMsg, setErrorMsg] = useState("");
  const [created, setCreated] = useState<CreatedCompany | null>(null);
  const [turnstileToken, setTurnstileToken] = useState("");
  const [conversationId, setConversationId] = useState<string | null>(null);

  const turnstileRef = useRef<HTMLDivElement>(null);
  const widgetIdRef = useRef<string | null>(null);
  const transcriptRef = useRef<HTMLDivElement>(null);

  // Skip Turnstile on localhost — sitekeys are domain-bound, so the prod key
  // returns 401 from challenges.cloudflare.com when the page origin doesn't
  // match the registered domains. The /api/demo-chat route only verifies the
  // token if one is provided, so passing none falls through cleanly in dev.
  const isLocalHost =
    typeof window !== "undefined" &&
    (window.location.hostname === "localhost" ||
      window.location.hostname === "127.0.0.1" ||
      window.location.hostname.endsWith(".local"));
  const turnstileSiteKey = !isLocalHost
    ? process.env.NEXT_PUBLIC_TURNSTILE_SITE_KEY
    : undefined;

  const renderTurnstile = useCallback(() => {
    if (!turnstileRef.current || !window.turnstile || !turnstileSiteKey) return;
    if (widgetIdRef.current !== null) {
      window.turnstile.remove(widgetIdRef.current);
    }
    widgetIdRef.current = window.turnstile.render(turnstileRef.current, {
      sitekey: turnstileSiteKey,
      theme: "auto",
      size: "invisible",
      callback: (token: string) => setTurnstileToken(token),
      "expired-callback": () => setTurnstileToken(""),
      "error-callback": () => setTurnstileToken(""),
    });
  }, [turnstileSiteKey]);

  useEffect(() => {
    if (!open) return;
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      try {
        const parsed: CreatedCompany = JSON.parse(stored);
        if (new Date(parsed.expiresAt) > new Date()) {
          setCreated(parsed);
          setStatus("created");
        } else {
          localStorage.removeItem(STORAGE_KEY);
        }
      } catch {
        /* ignore */
      }
    }
  }, [open]);

  useEffect(() => {
    if (!open || !turnstileSiteKey) return;
    if (window.turnstile) {
      renderTurnstile();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";
    script.async = true;
    script.onload = () => renderTurnstile();
    document.head.appendChild(script);
    return () => {
      if (widgetIdRef.current !== null && window.turnstile) {
        window.turnstile.remove(widgetIdRef.current);
        widgetIdRef.current = null;
      }
    };
  }, [open, renderTurnstile, turnstileSiteKey]);

  useEffect(() => {
    transcriptRef.current?.scrollTo({ top: transcriptRef.current.scrollHeight, behavior: "smooth" });
  }, [messages, status]);

  if (!flags.demoChat) return null;

  async function handleSend(e: FormEvent) {
    e.preventDefault();
    const trimmed = input.trim();
    if (!trimmed || status === "sending" || status === "creating") return;
    setInput("");
    setMessages((m) => [...m, { role: "user", text: trimmed }]);
    setStatus("sending");
    setErrorMsg("");

    try {
      const res = await fetch("/api/demo-chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          action: "chat",
          message: trimmed,
          conversationId: conversationId ?? undefined,
          turnstileToken: turnstileToken || undefined,
        }),
      });
      const data = await res.json();
      if (!res.ok) {
        setStatus("error");
        setErrorMsg(data.error || "Something went wrong.");
        return;
      }
      // The AI service wraps responses as { success, data: { response, conversationId, traceId, toolResults } }.
      // The bare data object is the fallback for any path that doesn't go through the wrapper.
      const payload = data?.data ?? data;
      const newConversationId = payload?.conversationId ?? payload?.ConversationId;
      if (newConversationId) setConversationId(newConversationId);

      // create_demo_company is flagged AlwaysDirectReturnTools server-side, so its result
      // bubbles up via toolResults and we transition straight to the created state without
      // relying on the LLM to hand us anything in the natural-language reply.
      type ToolResult = { tool?: string; Tool?: string; result?: unknown; Result?: unknown };
      const toolResults: ToolResult[] | undefined =
        payload?.toolResults ?? payload?.ToolResults;
      const demoResult = toolResults?.find(
        (t) => (t.tool ?? t.Tool) === "create_demo_company",
      );
      const demoData = (demoResult?.result ?? demoResult?.Result) as
        | {
            success?: boolean;
            companyUId?: string;
            companyName?: string;
            demoExpiresAt?: string;
            claimToken?: string;
            traceId?: string;
            error?: string;
          }
        | undefined;

      if (demoData?.success && demoData.companyUId) {
        const c: CreatedCompany = {
          uid: demoData.companyUId,
          name: demoData.companyName ?? "(unnamed)",
          expiresAt: demoData.demoExpiresAt ?? new Date(Date.now() + 60 * 60 * 1000).toISOString(),
          claimToken: demoData.claimToken ?? "",
          traceId: demoData.traceId ?? "",
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(c));
        setCreated(c);
        setStatus("created");
        return;
      }

      if (demoData && demoData.success === false) {
        setStatus("error");
        setErrorMsg(demoData.error ?? "Demo company creation failed.");
        return;
      }

      const reply = payload?.response ?? payload?.message ?? "(no reply)";
      setMessages((m) => [...m, { role: "assistant", text: reply }]);
      setStatus("idle");
    } catch (err) {
      console.error("[DemoChat] chat send failed", err);
      setStatus("error");
      setErrorMsg("Network error. Please try again.");
    }
  }

  async function runCreate(companyName: string) {
    setStatus("creating");
    try {
      const res = await fetch("/api/demo-chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          action: "create",
          companyName,
        }),
      });
      const data = await res.json();
      if (!res.ok) {
        setStatus("error");
        setErrorMsg(data.error || "Failed to create demo company.");
        return;
      }
      const payload = data?.data ?? data;
      const c: CreatedCompany = {
        uid: payload.company?.id,
        name: payload.company?.name,
        expiresAt: payload.demoExpiresAt,
        claimToken: payload.claimToken,
        traceId: payload.traceId,
      };
      localStorage.setItem(STORAGE_KEY, JSON.stringify(c));
      setCreated(c);
      setStatus("created");
    } catch (err) {
      console.error("[DemoChat] create failed", err);
      setStatus("error");
      setErrorMsg("Network error during creation.");
    }
  }

  async function handleClaimClick() {
    if (!created) return;
    const keycloak = process.env.NEXT_PUBLIC_KEYCLOAK_AUTHORITY;
    const clientId = process.env.NEXT_PUBLIC_KEYCLOAK_CLAIM_CLIENT_ID ?? "landing-claim";

    // No Keycloak configured? Fall through to the callback page in dev so
    // the wiring is at least testable manually with hand-crafted params.
    if (!keycloak) {
      window.location.href =
        `/claim-callback?companyUId=${created.uid}&token=${encodeURIComponent(created.claimToken)}`;
      return;
    }

    // PKCE: code_verifier stays in sessionStorage and is exchanged for
    // tokens on the callback. No client secret on the landing — Keycloak
    // verifies the verifier hash matches the challenge we sent here.
    const codeVerifier = generateRandomString(64);
    const codeChallenge = await pkceChallenge(codeVerifier);
    const state = generateRandomString(32);

    sessionStorage.setItem("demoClaim.pkce", JSON.stringify({
      codeVerifier,
      state,
      claimToken: created.claimToken,
      companyUId: created.uid,
      expiresAt: Date.now() + 10 * 60 * 1000,
    }));

    const redirectUri = `${window.location.origin}/claim-callback`;
    const params = new URLSearchParams({
      client_id: clientId,
      response_type: "code",
      scope: "openid email profile",
      redirect_uri: redirectUri,
      state,
      code_challenge: codeChallenge,
      code_challenge_method: "S256",
      // Keycloak shortcut: send the visitor straight to signup. Falls back
      // to the login screen if they already have an account.
      kc_action: "register",
    });

    window.location.href = `${keycloak.replace(/\/$/, "")}/protocol/openid-connect/auth?${params}`;
  }

  function jaegerUrl(traceId: string) {
    return `https://jaeger.elkhair.tech/trace/${traceId}`;
  }

  function grafanaUrl(traceId: string) {
    return `https://grafana.elkhair.tech/d/bf5m5dwukfncwd/find-by-trace-id?var-TraceId=${traceId}`;
  }

  return (
    <>
      <button
        type="button"
        className="demo-chat-bubble"
        aria-label={open ? "Close demo chat" : "Open demo chat"}
        onClick={() => setOpen((o) => !o)}
      >
        {open ? "×" : "Try the demo"}
      </button>

      {open && (
        <div className="demo-chat-panel" role="dialog" aria-label="Demo chat">
          <div className="demo-chat-header">
            <strong>Create a demo company</strong>
            <button
              type="button"
              className="demo-chat-close"
              aria-label="Close"
              onClick={() => setOpen(false)}
            >
              ×
            </button>
          </div>

          <div ref={transcriptRef} className="demo-chat-transcript">
            {messages.length === 0 && status !== "created" && (
              <p className="demo-chat-empty">
                Hi! I&apos;ll walk you through creating a real demo company on the platform —
                the saga, Keycloak provisioning, and traces will all light up. Auto-deletes in
                1 hour. Tell me a name to get started.
              </p>
            )}
            {messages.map((m, i) => (
              <div key={i} className={`demo-chat-msg demo-chat-msg-${m.role}`}>
                {m.text}
              </div>
            ))}
            {status === "creating" && (
              <div className="demo-chat-msg demo-chat-msg-assistant">
                Provisioning the company across the saga…
              </div>
            )}
            {status === "created" && created && (
              <div className="demo-chat-success">
                <p>
                  <strong>{created.name}</strong> is live. Auto-deletes at{" "}
                  {new Date(created.expiresAt).toLocaleTimeString()}.
                </p>
                <div className="demo-chat-links">
                  <a href={jaegerUrl(created.traceId)} target="_blank" rel="noopener noreferrer">
                    View in Jaeger →
                  </a>
                  <a href={grafanaUrl(created.traceId)} target="_blank" rel="noopener noreferrer">
                    View in Grafana →
                  </a>
                  <button type="button" className="btn btn-primary" onClick={handleClaimClick}>
                    Claim this company
                  </button>
                </div>
              </div>
            )}
            {status === "error" && <p className="demo-chat-error">{errorMsg}</p>}
          </div>

          {status !== "created" && (
            <form className="demo-chat-input" onSubmit={handleSend}>
              <input
                type="text"
                value={input}
                onChange={(e) => setInput(e.target.value)}
                placeholder="Type a company name…"
                disabled={status === "sending" || status === "creating"}
              />
              <button
                type="submit"
                className="btn btn-primary"
                disabled={!input.trim() || status === "sending" || status === "creating"}
              >
                {status === "sending" ? "…" : "Send"}
              </button>
            </form>
          )}

          <div ref={turnstileRef} aria-hidden="true" />
        </div>
      )}
    </>
  );
}
