import {computed, inject, Injectable, signal} from '@angular/core';
import {FeatureFlagsDto} from '../types/Dtos/FeatureFlagsDto';
import {ActivityLogger} from './activity-logger.service';

const MODE_STORAGE_KEY = 'job-admin-mode-override';

/**
 * Lowercase every key in a flags dict so downstream lookups are case-insensitive.
 * Redis stores flags with mixed casing (e.g. `Monolith`, `ContactForm`, `deepDives`);
 * the monolith's JSON serializer applies camelCase by default, but this lets the
 * admin app stay correct regardless of how the server ever shapes the payload.
 */
function normalizeFlags(flags: FeatureFlagsDto | null): FeatureFlagsDto | null {
  if (!flags) return flags;
  const out: FeatureFlagsDto = {};
  for (const [k, v] of Object.entries(flags)) {
    out[k.toLowerCase()] = v;
  }
  return out;
}

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly logger = inject(ActivityLogger);

  private readonly _featureFlags = signal<FeatureFlagsDto|null>(null);
  private readonly _localOverride = signal<boolean | null>(this.readStoredOverride());
  private readonly _loaded = signal(false);

  readonly featureFlags = this._featureFlags.asReadonly();

  readonly hasLocalOverride = computed(() => this._localOverride() !== null);

  readonly globalDefault = computed(() => this._featureFlags()?.['monolith'] ?? false);

  readonly isMonolith = computed(() => {
    const override = this._localOverride();
    if (override !== null) return override;
    if (!this._loaded()) return false;
    return this._featureFlags()?.['monolith'] ?? false;
  });

  /** Whether the in-app contact form is enabled. Gates the nav link + route guard. */
  readonly contactForm = computed(() => this._featureFlags()?.['contactform'] ?? false);

  setFlags(flags: FeatureFlagsDto) {
    const normalized = normalizeFlags(flags);
    this._featureFlags.set(normalized);
    this._loaded.set(true);
    this.logger.info('feature flags received', {
      count: normalized ? Object.keys(normalized).length : 0,
    });
  }

  setLocalOverride(isMonolith: boolean) {
    localStorage.setItem(MODE_STORAGE_KEY, JSON.stringify(isMonolith));
    this._localOverride.set(isMonolith);
    this.logger.info('mode override set', { isMonolith });
  }

  clearLocalOverride() {
    localStorage.removeItem(MODE_STORAGE_KEY);
    this._localOverride.set(null);
    this.logger.info('mode override cleared');
  }

  private readStoredOverride(): boolean | null {
    const stored = localStorage.getItem(MODE_STORAGE_KEY);
    if (stored === null) return null;
    return stored === 'true';
  }
}
