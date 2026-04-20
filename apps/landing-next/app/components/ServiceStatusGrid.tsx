"use client";

import { useEffect, useState } from "react";
import { services } from "../data/portfolio-data";
import { useFeatureFlags } from "./FeatureFlags";

type Status = "up" | "degraded" | "unknown";

interface ServiceStatusEntry {
  name: string;
  status: Exclude<Status, "unknown">;
  latencyMs: number;
  httpStatus: number | null;
  reason?: string;
}

/**
 * Renders the `Running Services` card grid on the portfolio page.
 *
 * - When the `serviceStatus` feature flag is OFF (default): behaves exactly like
 *   the previous static markup — all dots green, no network activity.
 * - When it's ON: polls `/api/status` every 30s and stamps `data-status` on each
 *   dot (`up` → green, `degraded` → yellow, `unknown` → gray for initial load).
 *
 * Moved into a client component so the polling hook can live alongside the
 * feature-flag read from React context (FeatureFlagsProvider populates the flag
 * values at SSR time, so the flag state here is correct on first render).
 */
export function ServiceStatusGrid() {
  const flags = useFeatureFlags();
  const live = flags.serviceStatus;

  const [statuses, setStatuses] = useState<Record<string, ServiceStatusEntry>>({});

  useEffect(() => {
    if (!live) return;
    let cancelled = false;

    async function refresh() {
      try {
        const res = await fetch("/api/status", { cache: "no-store" });
        if (!res.ok || cancelled) return;
        const data: ServiceStatusEntry[] = await res.json();
        if (cancelled) return;
        const next: Record<string, ServiceStatusEntry> = {};
        for (const s of data) next[s.name] = s;
        setStatuses(next);
      } catch {
        /* swallow — keep last-known state; dots go `unknown` only on first load */
      }
    }

    refresh();
    const id = setInterval(refresh, 30_000);
    return () => {
      cancelled = true;
      clearInterval(id);
    };
  }, [live]);

  return (
    <div className="service-grid" aria-label="Service links">
      {services.map((s) => {
        // Flag off → render the original static green dot (no data-status attr)
        // Flag on → stamp data-status so globals.css can color the dot
        const entry = live ? statuses[s.name] : undefined;
        const status: Status | undefined = live
          ? (entry ? entry.status : "unknown")
          : undefined;
        const title = !live
          ? undefined
          : entry
            ? entry.status === "up"
              ? `Up · ${entry.latencyMs}ms`
              : `Degraded · ${entry.reason ?? "unreachable"}`
            : "Checking…";

        return (
          <a
            href={s.href}
            target="_blank"
            rel="noopener noreferrer"
            className="service-card"
            key={s.name}
          >
            <span
              className="service-dot"
              {...(status ? { "data-status": status } : {})}
              {...(title ? { title } : {})}
              aria-hidden="true"
            />
            <div>
              <div className="service-name">{s.name}</div>
              <div className="service-url">{s.url}</div>
            </div>
            <div
              className={`service-tooltip${s.tooltipClass ? ` ${s.tooltipClass}` : ""}`}
              dangerouslySetInnerHTML={{ __html: s.tooltip }}
            />
          </a>
        );
      })}
    </div>
  );
}
