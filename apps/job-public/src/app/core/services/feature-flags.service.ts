import {computed, Injectable, signal} from '@angular/core';

@Injectable({providedIn: 'root'})
export class FeatureFlagsService {
  private readonly _flags = signal<Record<string, boolean> | null>(null);

  readonly chatEnabled = computed(() => this._flags()?.['PublicChat'] ?? false);
  /** Whether the in-app contact form is enabled. Gates the nav link + route guard. */
  readonly contactForm = computed(() => this._flags()?.['ContactForm'] ?? false);

  setFlags(flags: Record<string, boolean>) {
    this._flags.set(flags);
  }
}
