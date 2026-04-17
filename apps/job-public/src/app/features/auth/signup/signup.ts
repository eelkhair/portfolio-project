import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { AccountService } from '../../../core/services/account.service';
import { environment } from '../../../../environments/environment';

interface SignupResponse {
  data?: { userId: string; email: string; groupPath: string };
  exceptions?: { message?: string; errors?: Record<string, string[]> };
  success: boolean;
}

/**
 * Cross-field validator: ensures `password` and `confirmPassword` match.
 * Emits a `passwordMismatch` error on the confirmPassword control when they differ.
 */
const passwordMatchValidator: ValidatorFn = (group: AbstractControl): ValidationErrors | null => {
  const pw = group.get('password')?.value;
  const cpw = group.get('confirmPassword');
  if (!cpw) return null;
  if (pw && cpw.value && pw !== cpw.value) {
    cpw.setErrors({ ...(cpw.errors ?? {}), passwordMismatch: true });
    return { passwordMismatch: true };
  }
  // Clear only the passwordMismatch error; preserve other errors (e.g. required).
  if (cpw.errors?.['passwordMismatch']) {
    const { passwordMismatch, ...rest } = cpw.errors;
    cpw.setErrors(Object.keys(rest).length ? rest : null);
  }
  return null;
};

/**
 * Self-signup for the public app. POSTs to the monolith's
 * `POST /api/Account/signup/public` endpoint which creates a Keycloak user in /Applicants.
 * After success, kicks off the OIDC login flow so the user lands authenticated.
 */
@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './signup.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Signup implements AfterViewInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly account = inject(AccountService);

  private readonly turnstileHost =
    viewChild<ElementRef<HTMLDivElement>>('turnstileHost');
  private readonly usernameInput =
    viewChild<ElementRef<HTMLInputElement>>('usernameInput');
  private turnstileWidgetId: string | null = null;

  protected readonly form = new FormGroup(
    {
      username: new FormControl('', {
        nonNullable: true,
        validators: [
          Validators.required,
          Validators.minLength(3),
          Validators.maxLength(32),
          Validators.pattern(/^[a-zA-Z0-9._-]+$/),
        ],
      }),
      firstName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(1)],
      }),
      lastName: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(1)],
      }),
      email: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.email],
      }),
      password: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required, Validators.minLength(8)],
      }),
      confirmPassword: new FormControl('', {
        nonNullable: true,
        validators: [Validators.required],
      }),
    },
    { validators: passwordMatchValidator }
  );

  protected readonly submitting = signal(false);
  protected readonly success = signal(false);
  protected readonly fieldErrors = signal<Record<string, string[]> | undefined>(
    undefined
  );
  protected readonly topError = signal<string | undefined>(undefined);
  protected readonly turnstileToken = signal<string>('');

  constructor() {
    // Focus the username input on mount. We do this in afterNextRender so it runs
    // after Angular's initial paint + hydration, AND schedule a fallback via
    // setTimeout so we beat Angular Router's post-navigation focus reset on SPA
    // navigation (e.g. clicking "Sign Up" in the header). HTML autofocus handles
    // direct page loads; these handle SPA nav.
    afterNextRender(() => {
      const focusUsername = () => this.usernameInput()?.nativeElement.focus();
      focusUsername();
      // Belt-and-suspenders: re-focus after the task queue drains in case the
      // router or another effect steals focus during hydration.
      setTimeout(focusUsername, 0);
      setTimeout(focusUsername, 100);
    });
  }

  ngAfterViewInit(): void {
    if (typeof window === 'undefined') return; // SSR guard
    this.loadTurnstile();
  }

  ngOnDestroy(): void {
    if (this.turnstileWidgetId && window.turnstile) {
      window.turnstile.remove(this.turnstileWidgetId);
      this.turnstileWidgetId = null;
    }
  }

  submit(): void {
    this.topError.set(undefined);
    this.fieldErrors.set(undefined);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const token = this.turnstileToken();
    if (!token) {
      this.topError.set('Please complete the captcha before submitting.');
      return;
    }

    this.submitting.set(true);

    // Strip confirmPassword before sending — backend doesn't need it.
    const { confirmPassword, ...values } = this.form.getRawValue();
    const body = { ...values, turnstileToken: token };
    const url = `${environment.apiUrl}Account/signup/public`;

    this.http.post<SignupResponse>(url, body).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
        setTimeout(() => this.account.login(), 800);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.resetTurnstile();
        const payload = err.error as SignupResponse | undefined;
        if (payload?.exceptions?.errors) {
          this.fieldErrors.set(payload.exceptions.errors);
        }
        this.topError.set(
          payload?.exceptions?.message ??
            (err.status === 409
              ? 'An account with this email or username already exists.'
              : err.status === 403
                ? 'Captcha verification failed. Please try again.'
                : 'Signup failed. Please try again.')
        );
      },
    });
  }

  goToLogin(): void {
    this.account.login();
  }

  private loadTurnstile(): void {
    if (window.turnstile) {
      this.renderWidget();
      return;
    }
    const script = document.createElement('script');
    script.src =
      'https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit';
    script.async = true;
    script.defer = true;
    script.onload = () => this.renderWidget();
    document.head.appendChild(script);
  }

  private renderWidget(): void {
    const host = this.turnstileHost()?.nativeElement;
    if (!host || !window.turnstile) return;
    if (this.turnstileWidgetId) {
      window.turnstile.remove(this.turnstileWidgetId);
    }
    this.turnstileWidgetId = window.turnstile.render(host, {
      sitekey: environment.turnstileSiteKey,
      theme: 'auto',
      callback: (token: string) => this.turnstileToken.set(token),
      'expired-callback': () => this.turnstileToken.set(''),
      'error-callback': () => this.turnstileToken.set(''),
    });
  }

  private resetTurnstile(): void {
    this.turnstileToken.set('');
    if (this.turnstileWidgetId && window.turnstile) {
      window.turnstile.reset(this.turnstileWidgetId);
    }
  }
}
