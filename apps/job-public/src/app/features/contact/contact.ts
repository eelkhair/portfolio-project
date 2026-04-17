import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import {
  FormControl,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { AccountService } from '../../core/services/account.service';
import { environment } from '../../../environments/environment';

interface ContactResponse {
  success?: boolean;
  error?: string;
}

/**
 * In-app contact form for authenticated public-app users. Submits to the
 * landing-next `/api/contact` route cross-origin. That Next.js route
 * handles Turnstile verification, rate limiting, and SMTP delivery via
 * Exchange to the operations inbox — avoiding coupling app mail to the
 * monolith's SMTP (which points to Mailpit in dev/prod).
 *
 * Feature-flag gated: route is only reachable when `featureFlags.contactForm()`
 * is true (see `contactEnabledGuard`).
 */
@Component({
  selector: 'app-contact',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './contact.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Contact implements AfterViewInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly account = inject(AccountService);

  private readonly turnstileHost =
    viewChild<ElementRef<HTMLDivElement>>('turnstileHost');
  private readonly subjectInput =
    viewChild<ElementRef<HTMLInputElement>>('subjectInput');
  private turnstileWidgetId: string | null = null;

  protected readonly form = new FormGroup({
    subject: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    message: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.minLength(10),
        Validators.maxLength(5000),
      ],
    }),
  });

  protected readonly submitting = signal(false);
  protected readonly success = signal(false);
  protected readonly topError = signal<string | undefined>(undefined);
  protected readonly turnstileToken = signal<string>('');

  /** Pre-filled from the authenticated user's OIDC profile — shown but not editable. */
  protected readonly fromName = computed(() => {
    const u = this.account.user();
    const name = (u?.['name'] as string | undefined)?.trim();
    if (name) return name;
    const composed = [u?.['given_name'], u?.['family_name']]
      .filter(Boolean)
      .join(' ')
      .trim();
    if (composed) return composed;
    return (u?.['preferred_username'] as string | undefined) ?? 'User';
  });

  protected readonly fromEmail = computed(() => {
    const u = this.account.user();
    return (u?.['email'] as string | undefined) ?? '';
  });

  constructor() {
    // Focus the subject input after initial render. Belt-and-suspenders
    // mirrors the signup pattern to beat SPA-navigation focus resets.
    afterNextRender(() => {
      const focusSubject = () => this.subjectInput()?.nativeElement.focus();
      focusSubject();
      setTimeout(focusSubject, 0);
      setTimeout(focusSubject, 100);
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
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const token = this.turnstileToken();
    if (!token) {
      this.topError.set('Please complete the captcha before submitting.');
      return;
    }

    const name = this.fromName();
    const email = this.fromEmail();
    if (!email) {
      this.topError.set(
        'Your account has no email on file. Please update your profile first.',
      );
      return;
    }

    this.submitting.set(true);

    const { subject, message } = this.form.getRawValue();
    const body = { name, email, subject, message, token };
    const url = `${environment.landingUrl}api/contact`;

    this.http.post<ContactResponse>(url, body).subscribe({
      next: () => {
        this.submitting.set(false);
        this.success.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.resetTurnstile();
        const payload = err.error as ContactResponse | undefined;
        this.topError.set(
          payload?.error ??
            (err.status === 429
              ? 'Please wait a minute before sending another message.'
              : err.status === 403
                ? 'Captcha verification failed. Please try again.'
                : 'Failed to send. Please try again.'),
        );
      },
    });
  }

  sendAnother(): void {
    this.success.set(false);
    this.form.reset();
    // The form (and turnstile host div) unmounted while success was true —
    // viewChild now references a brand-new empty host on re-render. The
    // previous widget is orphaned; drop the stale ID and render a fresh
    // widget once Angular has re-mounted the form.
    this.turnstileToken.set('');
    this.turnstileWidgetId = null;
    setTimeout(() => {
      this.renderWidget();
      this.subjectInput()?.nativeElement.focus();
    }, 0);
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
      callback: (t: string) => this.turnstileToken.set(t),
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
