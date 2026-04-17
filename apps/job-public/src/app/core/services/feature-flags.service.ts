import {computed, Injectable, signal} from '@angular/core';

/**
 * Lowercase every key in a flags dict so downstream lookups are case-insensitive.
 * Redis stores flags with mixed casing; the monolith's JSON serializer applies
 * camelCase by default — normalizing to lowercase keeps this service resilient
 * to both the server casing and any future changes.
 */
function normalizeFlags(flags: Record<string, boolean> | null): Record<string, boolean> | null {
  if (!flags) return flags;
  const out: Record<string, boolean> = {};
  for (const [k, v] of Object.entries(flags)) {
    out[k.toLowerCase()] = v;
  }
  return out;
}

@Injectable({providedIn: 'root'})
export class FeatureFlagsService {
  private readonly _flags = signal<Record<string, boolean> | null>(null);

  readonly chatEnabled = computed(() => this._flags()?.['publicchat'] ?? false);
  /** Whether the in-app contact form is enabled. Gates the nav link + route guard. */
  readonly contactForm = computed(() => this._flags()?.['contactform'] ?? false);

  setFlags(flags: Record<string, boolean>) {
    this._flags.set(normalizeFlags(flags));
  }
}
