import { HttpContextToken } from '@angular/common/http';
export const IDEMPOTENCY_DISABLE = new HttpContextToken<boolean>(() => false);
export const IDEMPOTENCY_FORCE_KEY = new HttpContextToken<string | null>(() => null);
