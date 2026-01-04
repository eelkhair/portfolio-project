import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface LocationValidatorOptions {
  keywords?: string[];      // defaults: ['Remote','Hybrid']
  requireUSState?: boolean; // keep true for City, ST
}

export function locationValidator(opts: LocationValidatorOptions = {}): ValidatorFn {
  const keywords = (opts.keywords ?? ['Remote', 'Hybrid']).map(k => k.toLowerCase());
  const cityState = /^[A-Za-z .'-]+,\s*[A-Za-z]{2}$/; // e.g., "San Jose, CA"

  return (c: AbstractControl): ValidationErrors | null => {
    const raw = (c.value ?? '').toString().trim();
    if (!raw) return null; // optional field

    // keyword checks (Remote/Hybrid)
    if (keywords.includes(raw.toLowerCase())) return null;

    // City, ST format
    if (cityState.test(raw)) return null;

    return { locationFormat: true };
  };
}

export function normalizeLocation(raw: string | null | undefined): string {
  if (!raw) return '';
  const v = raw.trim();

  // Keywords → Title case
  if (/^(remote|hybrid)$/i.test(v)) {
    return v[0].toUpperCase() + v.slice(1).toLowerCase();
  }

  // City, ST → "City, ST" (title-case city, uppercase state)
  const m = v.match(/^(.+),\s*([A-Za-z]{2})$/);
  if (!m) return v;
  const city = m[1].toLowerCase().replace(/\b\w/g, ch => ch.toUpperCase());
  const st = m[2].toUpperCase();
  return `${city}, ${st}`;
}
