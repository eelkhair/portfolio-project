"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";

interface PendingClaim {
  codeVerifier: string;
  state: string;
  claimToken: string;
  companyUId: string;
  expiresAt: number;
}

export default function ClaimCallbackPage() {
  const search = useSearchParams();
  const [status, setStatus] = useState<"working" | "success" | "error">("working");
  const [error, setError] = useState("");

  useEffect(() => {
    const code = search.get("code");
    const state = search.get("state");

    // Manual / dev fallback: legacy URL-param flow lets us test the claim
    // path without a Keycloak round-trip. If no `code` is present, fall
    // back to reading visitor data straight off the URL.
    if (!code) {
      manualClaim();
      return;
    }

    let pending: PendingClaim | null = null;
    try {
      const raw = sessionStorage.getItem("demoClaim.pkce");
      if (raw) pending = JSON.parse(raw);
    } catch {
      /* ignore */
    }

    if (!pending) {
      setError("Claim session expired or missing. Please start the demo again.");
      setStatus("error");
      return;
    }

    if (pending.expiresAt < Date.now()) {
      sessionStorage.removeItem("demoClaim.pkce");
      setError("Claim session expired. Please start the demo again.");
      setStatus("error");
      return;
    }

    if (pending.state !== state) {
      sessionStorage.removeItem("demoClaim.pkce");
      setError("Authorization state mismatch — possible session tampering.");
      setStatus("error");
      return;
    }

    fetch("/api/claim-callback", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        code,
        codeVerifier: pending.codeVerifier,
        claimToken: pending.claimToken,
        companyUId: pending.companyUId,
        redirectUri: `${window.location.origin}/claim-callback`,
      }),
    })
      .then(async (res) => {
        const data = await res.json();
        sessionStorage.removeItem("demoClaim.pkce");
        if (!res.ok) {
          setError(data.error || "Failed to claim demo company.");
          setStatus("error");
          return;
        }
        setStatus("success");
        const adminUrl = process.env.NEXT_PUBLIC_JOB_ADMIN_URL ?? "https://job-admin.elkhair.tech";
        setTimeout(() => {
          window.location.href = adminUrl;
        }, 2000);
      })
      .catch((err) => {
        sessionStorage.removeItem("demoClaim.pkce");
        console.error("[ClaimCallback] failed", err);
        setError("Network error during claim.");
        setStatus("error");
      });

    function manualClaim() {
      const companyUId = search.get("companyUId");
      const token = search.get("token");
      if (!companyUId || !token) {
        setError("Missing claim parameters.");
        setStatus("error");
        return;
      }
      const keycloakUserId = search.get("keycloakUserId") ?? "";
      const email = search.get("email") ?? "";
      const firstName = search.get("firstName") ?? "";
      const lastName = search.get("lastName") ?? "";

      fetch("/api/demo-chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          action: "claim",
          companyUId,
          claimToken: token,
          keycloakUserId,
          email,
          firstName,
          lastName,
        }),
      })
        .then(async (res) => {
          const data = await res.json();
          if (!res.ok) {
            setError(data.error || "Failed to claim demo company.");
            setStatus("error");
            return;
          }
          setStatus("success");
          const adminUrl = process.env.NEXT_PUBLIC_JOB_ADMIN_URL ?? "https://job-admin.elkhair.tech";
          setTimeout(() => {
            window.location.href = adminUrl;
          }, 2000);
        })
        .catch((err) => {
          console.error("[ClaimCallback] failed", err);
          setError("Network error during claim.");
          setStatus("error");
        });
    }
  }, [search]);

  return (
    <main style={{ padding: "4rem 2rem", textAlign: "center" }}>
      {status === "working" && <p>Claiming your demo company…</p>}
      {status === "success" && (
        <>
          <h1>Welcome aboard!</h1>
          <p>Redirecting you to the admin app…</p>
        </>
      )}
      {status === "error" && (
        <>
          <h1>Something went wrong</h1>
          <p>{error}</p>
        </>
      )}
    </main>
  );
}
