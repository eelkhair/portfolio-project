import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { RouterLink } from '@angular/router';
import { OidcSecurityService } from 'angular-auth-oidc-client';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';
import { Password } from 'primeng/password';
import { Message } from 'primeng/message';
import { environment } from '../../../../environments/environment';

interface SignupResponse {
  data?: { userId: string; email: string; groupPath: string };
  exceptions?: { message?: string; errors?: Record<string, string[]> };
  success: boolean;
}

/**
 * Cross-field validator: ensures `password` and `confirmPassword` match.
 * Emits a `passwordMismatch` error on the confirmPassword control when they differ;
 * clears it (while preserving other errors) when they match.
 */
const passwordMatchValidator: ValidatorFn = (
  group: AbstractControl
): ValidationErrors | null => {
  const pw = group.get('password')?.value;
  const cpw = group.get('confirmPassword');
  if (!cpw) return null;
  if (pw && cpw.value && pw !== cpw.value) {
    cpw.setErrors({ ...(cpw.errors ?? {}), passwordMismatch: true });
    return { passwordMismatch: true };
  }
  if (cpw.errors?.['passwordMismatch']) {
    const { passwordMismatch, ...rest } = cpw.errors;
    cpw.setErrors(Object.keys(rest).length ? rest : null);
  }
  return null;
};

/**
 * Self-signup for admins. POSTs to `/api/Account/signup/admin` which is served by
 * either the monolith or the admin-api microservice depending on the `x-mode` header
 * set by `TracingInterceptor`. Both paths create a Keycloak user in `/Admins`.
 *
 * After success the component kicks off the OIDC authorize flow so the user lands
 * authenticated in `/dashboard`.
 */
@Component({
  selector: 'app-signup',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    RouterLink,
    Button,
    InputText,
    Password,
    Message,
  ],
  templateUrl: './signup.html',
  styleUrl: './signup.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Signup implements AfterViewInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly oidc = inject(OidcSecurityService);

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

  ngAfterViewInit(): void {
    this.loadTurnstile();
    // Autofocus username field on entry (next tick so PrimeNG's Password wrapper
    // has finished hydrating and won't grab focus back).
    queueMicrotask(() => this.usernameInput()?.nativeElement.focus());
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
    const url = `${environment.gatewayUrl}api/Account/signup/admin`;

    this.http.post<SignupResponse>(url, body).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
        setTimeout(() => this.oidc.authorize(), 800);
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
    this.oidc.authorize();
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
