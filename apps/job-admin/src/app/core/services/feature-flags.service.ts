import {computed, Injectable, signal} from '@angular/core';
import {FeatureFlagsDto} from '../types/Dtos/FeatureFlagsDto';

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly _featureFlags = signal<FeatureFlagsDto|null>(null);

  readonly featureFlags = this._featureFlags.asReadonly();
  private readonly _loaded = signal(false);

  setFlags(flags: FeatureFlagsDto) {
    this._featureFlags.set(flags);
    this._loaded.set(true);
  }

  isMonolith = computed(() => {
    if (!this._loaded()) return false; // or throw
    return this._featureFlags()?.['Monolith']?? false;
  });

}
