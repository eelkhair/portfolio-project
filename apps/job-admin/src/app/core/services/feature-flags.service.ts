import {computed, Injectable, signal} from '@angular/core';
import {FeatureFlagsDto} from '../types/Dtos/FeatureFlagsDto';

const MODE_STORAGE_KEY = 'job-admin-mode-override';

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly _featureFlags = signal<FeatureFlagsDto|null>(null);
  private readonly _localOverride = signal<boolean | null>(this.readStoredOverride());
  private readonly _loaded = signal(false);

  readonly featureFlags = this._featureFlags.asReadonly();

  readonly hasLocalOverride = computed(() => this._localOverride() !== null);

  readonly globalDefault = computed(() => this._featureFlags()?.['Monolith'] ?? false);

  readonly isMonolith = computed(() => {
    const override = this._localOverride();
    if (override !== null) return override;
    if (!this._loaded()) return false;
    return this._featureFlags()?.['Monolith'] ?? false;
  });

  setFlags(flags: FeatureFlagsDto) {
    this._featureFlags.set(flags);
    this._loaded.set(true);
  }

  setLocalOverride(isMonolith: boolean) {
    localStorage.setItem(MODE_STORAGE_KEY, JSON.stringify(isMonolith));
    this._localOverride.set(isMonolith);
  }

  clearLocalOverride() {
    localStorage.removeItem(MODE_STORAGE_KEY);
    this._localOverride.set(null);
  }

  private readStoredOverride(): boolean | null {
    const stored = localStorage.getItem(MODE_STORAGE_KEY);
    if (stored === null) return null;
    return stored === 'true';
  }
}
