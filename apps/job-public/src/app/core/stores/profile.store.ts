import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import mammoth from 'mammoth';
import { propagation, ROOT_CONTEXT, SpanKind, trace } from '@opentelemetry/api';
import { ApiService } from '../services/api.service';
import { MatchingJob } from '../types/job.type';
import { ResumeData, ResumeResponse, UserProfile, UserProfileRequest } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ProfileStore {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly tracer = trace.getTracer('public-fe');
  private lastTraceParent: string | undefined;

  readonly profile = signal<UserProfile | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  // Resume state
  readonly resumes = signal<ResumeResponse[]>([]);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);

  // Resume parse confirmation (profile page)
  readonly profileParseStatus = signal<'idle' | 'parsing' | 'ready' | 'applied' | 'error' | 'retrying'>('idle');
  readonly pendingParsedContent = signal<ResumeData | null>(null);
  readonly lastUploadedResumeId = signal<string | null>(null);
  readonly lastUploadedFileName = signal('');

  // Default resume parsed content (for summary display)
  readonly defaultResumeParsedContent = signal<ResumeData | null>(null);

  // Matching jobs state
  readonly matchingJobs = signal<MatchingJob[]>([]);
  readonly matchingJobsLoading = signal(false);
  readonly matchingJobsError = signal<string | null>(null);

  // Preview state
  readonly previewResume = signal<ResumeResponse | null>(null);
  readonly previewUrl = signal<string | null>(null);
  readonly previewHtml = signal<string | null>(null);
  readonly previewLoading = signal(false);
  readonly previewError = signal<string | null>(null);

  loadProfile(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getProfile().subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false); // Profile may not exist yet
      },
    });
  }

  saveProfile(request: UserProfileRequest): void {
    this.saving.set(true);
    this.saved.set(false);
    this.error.set(null);

    this.api.upsertProfile(request).subscribe({
      next: (profile) => {
        this.profile.set(profile);
        this.saving.set(false);
        this.saved.set(true);

        setTimeout(() => this.saved.set(false), 3000);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err?.error?.exceptions?.message ?? 'Failed to save profile.');
      },
    });
  }

  loadResumes(andMatchJobs = false): void {
    this.api.getResumes().subscribe({
      next: (resumes) => {
        this.resumes.set(resumes);
        const defaultResume = resumes.find((r) => r.isDefault && r.hasParsedContent);
        if (defaultResume) {
          this.loadDefaultResumeParsedContent(defaultResume.id);
        }
        if (andMatchJobs && resumes.some((r) => r.isDefault)) {
          this.loadMatchingJobs();
        }
      },
      error: () => {}, // Silently fail — resumes may not exist yet
    });
  }

  private loadDefaultResumeParsedContent(resumeId: string): void {
    this.api.getResumeParsedContent(resumeId).subscribe({
      next: (data) => this.defaultResumeParsedContent.set(data),
      error: () => {}, // Non-critical
    });
  }

  loadMatchingJobs(traceParent?: string): void {
    this.matchingJobsLoading.set(true);
    this.matchingJobsError.set(null);

    this.api.getMatchingJobs(10, traceParent).subscribe({
      next: (jobs) => {
        this.matchingJobs.set(jobs);
        this.matchingJobsLoading.set(false);
      },
      error: (err) => {
        // 404 = no default resume / no embeddings yet
        // 401 = token not ready (race with auth interceptor)
        // Both are expected — silently show empty state
        if (err?.status === 404 || err?.status === 401) {
          this.matchingJobs.set([]);
        } else {
          this.matchingJobsError.set(
            err?.error?.exceptions?.message ?? 'Failed to load matching jobs.'
          );
        }
        this.matchingJobsLoading.set(false);
      },
    });
  }

  uploadResume(file: File): void {
    this.uploading.set(true);
    this.uploadError.set(null);
    this.pendingParsedContent.set(null);
    this.lastUploadedFileName.set(file.name);

    const currentPage = this.router.url;

    this.api.uploadResume(file, currentPage).subscribe({
      next: (resume) => {
        this.resumes.update((list) => [resume, ...list]);
        this.uploading.set(false);
        this.lastUploadedResumeId.set(resume.id);
        this.profileParseStatus.set('parsing');
        // Parsed content arrives asynchronously via SignalR
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to upload resume.');
        this.profileParseStatus.set('error');
      },
    });
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeParsed" arrives */
  onResumeParsed(resumeId: string, traceParent?: string): void {
    if (this.lastUploadedResumeId() !== resumeId) return;
    this.lastTraceParent = traceParent;

    this.api.getResumeParsedContent(resumeId, traceParent).subscribe({
      next: (data) => {
        if (data) {
          this.pendingParsedContent.set(data);
          this.profileParseStatus.set('ready');
        }
      },
      error: () => this.profileParseStatus.set('error'),
    });
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeEmbedded" arrives */
  onResumeEmbedded(_resumeId: string, traceParent?: string): void {
    // Always refresh — the API uses the default resume regardless,
    // so a redundant call after a non-default embed is harmless.
    this.loadMatchingJobs(traceParent);
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeParseFailed" arrives */
  onResumeParseFailed(retrying: boolean): void {
    if (!this.lastUploadedResumeId()) return;
    this.profileParseStatus.set(retrying ? 'retrying' : 'error');
  }

  /** Recover from missed SignalR messages (tab backgrounded or reconnect) */
  recoverIfParsing(): void {
    const status = this.profileParseStatus();
    const id = this.lastUploadedResumeId();
    if (!id || (status !== 'parsing' && status !== 'retrying')) return;

    this.api.getResumeParsedContent(id).subscribe({
      next: (data) => {
        if (data) {
          this.pendingParsedContent.set(data);
          this.profileParseStatus.set('ready');
        }
        // null → still processing, stay in current state
      },
      error: () => this.profileParseStatus.set('error'),
    });
  }

  /** User confirms they want AI-parsed data applied to the profile form */
  applyParsedContent(): void {
    this.emitUserDecisionSpan('applied');
    // Update default resume summary from the parsed content
    const pending = this.pendingParsedContent();
    if (pending) {
      this.defaultResumeParsedContent.set(pending);
    }
    this.profileParseStatus.set('applied');
    // pendingParsedContent stays available for the component effect to read
  }

  /** User declines AI auto-fill */
  dismissParsedContent(): void {
    this.emitUserDecisionSpan('dismissed');
    this.pendingParsedContent.set(null);
    this.profileParseStatus.set('applied');
  }

  setDefaultResume(id: string): void {
    this.api.setDefaultResume(id).subscribe({
      next: () => {
        this.resumes.update((list) =>
          list.map((r) => ({ ...r, isDefault: r.id === id }))
        );
        this.loadMatchingJobs();
      },
      error: (err) => {
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to set default resume.');
      },
    });
  }

  deleteResume(id: string): void {
    const wasDefault = this.resumes().find((r) => r.id === id)?.isDefault;
    this.api.deleteResume(id).subscribe({
      next: () => {
        this.resumes.update((list) => list.filter((r) => r.id !== id));
        if (wasDefault) {
          this.matchingJobs.set([]);
        }
      },
      error: (err) => {
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to delete resume.');
      },
    });
  }

  openPreview(resume: ResumeResponse): void {
    this.previewResume.set(resume);
    this.previewLoading.set(true);
    this.previewError.set(null);
    this.previewUrl.set(null);
    this.previewHtml.set(null);

    this.api.downloadResumeBlob(resume.id).subscribe({
      next: (blob) => this.processPreviewBlob(blob, resume.contentType),
      error: (err) => {
        this.previewLoading.set(false);
        this.previewError.set(err?.error?.exceptions?.message ?? 'Failed to load resume.');
      },
    });
  }

  private async processPreviewBlob(blob: Blob, contentType?: string): Promise<void> {
    try {
      if (contentType === 'application/pdf') {
        this.previewUrl.set(URL.createObjectURL(blob));
      } else if (
        contentType === 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
      ) {
        const arrayBuffer = await blob.arrayBuffer();
        const result = await mammoth.convertToHtml({ arrayBuffer });
        this.previewHtml.set(result.value);
      } else if (contentType === 'text/plain') {
        const text = await blob.text();
        this.previewHtml.set(`<pre>${text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')}</pre>`);
      }
      this.previewLoading.set(false);
    } catch {
      this.previewLoading.set(false);
      this.previewError.set('Failed to render resume preview.');
    }
  }

  closePreview(): void {
    const url = this.previewUrl();
    if (url) {
      URL.revokeObjectURL(url);
    }
    this.previewResume.set(null);
    this.previewUrl.set(null);
    this.previewHtml.set(null);
    this.previewError.set(null);
  }

  downloadResume(resume: ResumeResponse): void {
    const existingUrl = this.previewUrl();
    if (existingUrl && this.previewResume()?.id === resume.id) {
      this.triggerDownload(existingUrl, resume.originalFileName);
      return;
    }

    this.api.downloadResumeBlob(resume.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        this.triggerDownload(url, resume.originalFileName);
        URL.revokeObjectURL(url);
      },
      error: (err) => {
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to download resume.');
      },
    });
  }

  private triggerDownload(url: string, fileName: string): void {
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
  }

  private emitUserDecisionSpan(decision: 'applied' | 'dismissed'): void {
    const parentCtx = this.extractTraceContext(this.lastTraceParent);
    const span = this.tracer.startSpan('resume.parse.user_decision', {
      kind: SpanKind.INTERNAL,
      attributes: {
        'resume.parse.user_action': decision,
        'resume.id': this.lastUploadedResumeId() ?? '',
        'resume.parse.page': 'profile',
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
}
