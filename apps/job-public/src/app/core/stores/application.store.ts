import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { switchMap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { propagation, ROOT_CONTEXT, SpanKind, trace } from '@opentelemetry/api';
import { ApiService } from '../services/api.service';
import { ApplicationStatus, PersonalInfoDto, WorkHistoryDto, EducationDto, CertificationDto, SubmitApplicationRequest } from '../types/application.type';
import { ParseStatus, ResumeData, UserProfile, UserProfileRequest } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ApplicationStore {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly tracer = trace.getTracer('public-fe');
  private lastTraceParent: string | undefined;

  readonly parseStatus = signal<ParseStatus>('idle');
  readonly resumeData = signal<ResumeData | null>(null);
  readonly pendingResumeData = signal<ResumeData | null>(null);
  readonly applicationStatus = signal<ApplicationStatus>('idle');
  readonly fileName = signal('');
  readonly resumeId = signal<string | null>(null);
  readonly profile = signal<UserProfile | null>(null);
  readonly profileLoaded = signal(false);
  readonly error = signal<string | null>(null);

  loadProfile(): void {
    this.api.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.profileLoaded.set(true);
      },
      error: () => this.profileLoaded.set(true), // Profile may not exist yet
    });
  }

  parseResume(file: File): void {
    this.fileName.set(file.name);
    this.parseStatus.set('uploading');

    const currentPage = this.router.url;

    this.api.uploadResume(file, currentPage).subscribe({
      next: (resume) => {
        this.resumeId.set(resume.id);
        this.parseStatus.set('parsing');
      },
      error: () => {
        this.parseStatus.set('error');
      },
    });
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeParsed" arrives */
  onResumeParsed(resumeId: string, currentPage?: string, traceParent?: string): void {
    // Only auto-load if the user is still on the same page
    const onSamePage = !currentPage || this.router.url === currentPage;
    if (onSamePage && this.resumeId() === resumeId) {
      this.lastTraceParent = traceParent;
      this.api.getResumeParsedContent(resumeId, traceParent).subscribe({
        next: (data) => {
          if (data) {
            this.pendingResumeData.set(data);
            this.parseStatus.set('ready');
          }
        },
        error: () => this.parseStatus.set('error'),
      });
    }
  }

  /** User confirms they want AI-parsed data applied to the form */
  applyResumeData(): void {
    const data = this.pendingResumeData();
    if (data) {
      this.emitUserDecisionSpan('applied');
      this.resumeData.set(data);
      this.pendingResumeData.set(null);
      this.parseStatus.set('parsed');
    }
  }

  /** User declines AI auto-fill, keeps their manual edits */
  dismissResumeData(): void {
    this.emitUserDecisionSpan('dismissed');
    this.pendingResumeData.set(null);
    this.parseStatus.set('parsed');
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeParseFailed" arrives */
  onResumeParseFailed(retrying: boolean): void {
    this.parseStatus.set(retrying ? 'retrying' : 'error');
  }

  /** Recover from missed SignalR messages (tab backgrounded or reconnect) */
  recoverIfParsing(): void {
    const status = this.parseStatus();
    const id = this.resumeId();
    if (!id || (status !== 'parsing' && status !== 'retrying')) return;

    this.api.getResumeParsedContent(id).subscribe({
      next: (data) => {
        if (data) {
          this.pendingResumeData.set(data);
          this.parseStatus.set('ready');
        }
        // null → still processing, stay in current state
      },
      error: () => this.parseStatus.set('error'),
    });
  }

  loadParsedContent(resumeId: string, fileName: string): void {
    this.fileName.set(fileName);
    this.resumeId.set(resumeId);
    this.resumeData.set(null);
    this.parseStatus.set('parsing');

    this.api.getResumeParsedContent(resumeId).subscribe({
      next: (data) => {
        if (data) {
          this.resumeData.set(data);
          this.parseStatus.set('parsed');
        } else {
          this.parseStatus.set('idle');
        }
      },
      error: () => {
        this.parseStatus.set('error');
      },
    });
  }

  submitApplication(
    jobId: string,
    coverLetter: string,
    profileData: UserProfileRequest,
    resumeId?: string,
    applicationData?: {
      personalInfo?: PersonalInfoDto;
      workHistory?: WorkHistoryDto[];
      education?: EducationDto[];
      certifications?: CertificationDto[];
      skills?: string[];
    },
  ): void {
    this.applicationStatus.set('submitting');
    this.error.set(null);

    const appRequest: SubmitApplicationRequest = {
      jobId,
      resumeId: resumeId || this.resumeId() || undefined,
      coverLetter: coverLetter || undefined,
      personalInfo: applicationData?.personalInfo,
      workHistory: applicationData?.workHistory,
      education: applicationData?.education,
      certifications: applicationData?.certifications,
      skills: applicationData?.skills,
    };

    // Sequential: save profile first, then submit application
    this.api.upsertProfile(profileData).pipe(
      switchMap((profile) => {
        this.profile.set(profile);
        return this.api.submitApplication(appRequest);
      }),
      catchError((err) => {
        this.applicationStatus.set('error');
        this.error.set(err?.error?.exceptions?.message ?? 'Failed to submit application.');
        throw err;
      }),
    ).subscribe({
      next: () => this.applicationStatus.set('submitted'),
      error: () => {}, // Already handled in catchError
    });
  }

  resetParse(): void {
    this.parseStatus.set('idle');
    this.resumeData.set(null);
    this.pendingResumeData.set(null);
    this.resumeId.set(null);
    this.fileName.set('');
    this.lastTraceParent = undefined;
  }

  private emitUserDecisionSpan(decision: 'applied' | 'dismissed'): void {
    const parentCtx = this.extractTraceContext(this.lastTraceParent);
    const span = this.tracer.startSpan('resume.parse.user_decision', {
      kind: SpanKind.INTERNAL,
      attributes: {
        'resume.parse.user_action': decision,
        'resume.id': this.resumeId() ?? '',
        'resume.parse.page': 'application',
      },
    }, parentCtx);
    span.end();
    this.lastTraceParent = undefined;
  }

  private extractTraceContext(traceParent?: string) {
    if (!traceParent) return undefined;
    const carrier: Record<string, string> = { traceparent: traceParent };
    return propagation.extract(ROOT_CONTEXT, carrier);
  }

  reset(): void {
    this.resetParse();
    this.applicationStatus.set('idle');
    this.profileLoaded.set(false);
    this.error.set(null);
  }
}
