import {computed, Injectable, signal} from '@angular/core';
import {FeatureFlagsDto} from '../types/Dtos/FeatureFlagsDto';

@Injectable({ providedIn: 'root' })
export class FeatureFlagsService {
  private readonly _featureFlags = signal<FeatureFlagsDto>({});

  readonly featureFlags = this._featureFlags.asReadonly();

  setFlags(flags: FeatureFlagsDto) {
    this._featureFlags.set(flags);
  }
  isTest1Enabled = computed(
    () => this._featureFlags()['Test1'] ?? false
  );

}
