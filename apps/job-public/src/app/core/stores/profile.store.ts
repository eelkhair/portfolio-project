import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import mammoth from 'mammoth';
import { propagation, ROOT_CONTEXT, SpanKind, trace } from '@opentelemetry/api';
import { ApiService } from '../services/api.service';
import { MatchingJob } from '../types/job.type';
import {
  ALL_RESUME_SECTIONS,
  ResumeData,
  ResumeResponse,
  ResumeSection,
  SECTION_LABELS,
  SectionStatus,
  UserProfile,
  UserProfileRequest,
} from '../types/resume-data.type';

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

  // Progressive parse state
  readonly profileParseStatus = signal<'idle' | 'parsing' | 'partial' | 'complete' | 'error' | 'retrying'>('idle');
  readonly sectionStatuses = signal<Record<ResumeSection, SectionStatus>>({
    contact: 'pending',
    skills: 'pending',
    workHistory: 'pending',
    education: 'pending',
    certifications: 'pending',
    projects: 'pending',
  });
  readonly progressiveParsedContent = signal<ResumeData | null>(null);
  readonly lastUploadedResumeId = signal<string | null>(null);
  readonly lastUploadedFileName = signal('');

  // Backward compat — old flow still uses these
  readonly pendingParsedContent = signal<ResumeData | null>(null);

  // Computed: how many sections are done or failed
  readonly sectionsComplete = computed(() => {
    const statuses = this.sectionStatuses();
    return ALL_RESUME_SECTIONS.filter(s => statuses[s] === 'done' || statuses[s] === 'failed').length;
  });

  readonly currentParsingSectionLabel = computed(() => {
    const statuses = this.sectionStatuses();
    const parsing = ALL_RESUME_SECTIONS.filter(s => statuses[s] === 'parsing');
    if (parsing.length === 0) return null;
    if (parsing.length === 1) return SECTION_LABELS[parsing[0]];
    return `${parsing.length} sections`;
  });

  // Default resume parsed content (for summary display)
  readonly defaultResumeParsedContent = signal<ResumeData | null>(null);

  // Matching jobs state
  readonly matchingJobs = signal<MatchingJob[]>([]);
  readonly matchingJobsLoading = signal(false);
  readonly matchingJobsError = signal<string | null>(null);

  // Populate from resume state
  readonly populatingFromResume = signal(false);

  readonly hasDefaultParsedResume = computed(() =>
    this.resumes().some((r) => r.isDefault && r.hasParsedContent)
  );

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
    const isInitialLoad = this.matchingJobs().length === 0;
    if (isInitialLoad) {
      this.matchingJobsLoading.set(true);
    }
    this.matchingJobsError.set(null);

    this.api.getMatchingJobs(10, traceParent).subscribe({
      next: (jobs) => {
        // Preserve existing explanations if the new data doesn't have them yet
        const existing = this.matchingJobs();
        if (existing.length > 0) {
          const existingMap = new Map(existing.map(j => [j.jobId, j]));
          const merged = jobs.map(j => {
            const prev = existingMap.get(j.jobId);
            if (prev && !j.matchSummary && prev.matchSummary) {
              return { ...j, matchSummary: prev.matchSummary, matchDetails: prev.matchDetails, matchGaps: prev.matchGaps };
            }
            return j;
          });
          this.matchingJobs.set(merged);
        } else {
          this.matchingJobs.set(jobs);
        }
        this.matchingJobsLoading.set(false);
      },
      error: (err) => {
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
    this.progressiveParsedContent.set(null);
    this.pendingParsedContent.set(null);
    this.lastUploadedFileName.set(file.name);

    // Reset section statuses
    this.sectionStatuses.set({
      contact: 'pending',
      skills: 'pending',
      workHistory: 'pending',
      education: 'pending',
      certifications: 'pending',
      projects: 'pending',
    });

    const currentPage = this.router.url;

    this.api.uploadResume(file, currentPage).subscribe({
      next: (resume) => {
        this.resumes.update((list) => [resume, ...list]);
        this.uploading.set(false);
        this.lastUploadedResumeId.set(resume.id);
        this.profileParseStatus.set('parsing');
        this.sectionStatuses.update(s => ({ ...s, contact: 'parsing' }));
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to upload resume.');
        this.profileParseStatus.set('error');
      },
    });
  }

  /** Called when SignalR "ResumeSectionParsed" arrives */
  onSectionParsed(resumeId: string, section: ResumeSection, traceParent?: string): void {
    if (this.lastUploadedResumeId() !== resumeId) return;
    this.lastTraceParent = traceParent;

    // Mark this section done
    this.sectionStatuses.update(s => ({ ...s, [section]: 'done' }));

    // When contact parse completes, kick all Phase 2 sections to 'parsing' (they run in parallel)
    if (section === 'contact') {
      this.sectionStatuses.update(s => ({
        ...s,
        skills: 'parsing',
        workHistory: 'parsing',
        education: 'parsing',
        certifications: 'parsing',
        projects: 'parsing',
      }));
    }

    this.profileParseStatus.set('partial');

    // Fetch latest merged parsed content from API and auto-apply
    this.api.getResumeParsedContent(resumeId, traceParent).subscribe({
      next: (data) => {
        if (data) {
          this.progressiveParsedContent.set(data);
          this.defaultResumeParsedContent.set(data);
        }
      },
      error: () => {}, // Non-critical — next section will re-fetch
    });
  }

  /** Called when SignalR "ResumeSectionFailed" arrives */
  onSectionFailed(resumeId: string, section: ResumeSection): void {
    if (this.lastUploadedResumeId() !== resumeId) return;
    this.sectionStatuses.update(s => ({ ...s, [section]: 'failed' }));
  }

  /** Called when SignalR "ResumeAllSectionsCompleted" arrives */
  onAllSectionsCompleted(resumeId: string, traceParent?: string): void {
    if (this.lastUploadedResumeId() !== resumeId) return;

    this.profileParseStatus.set('complete');

    // Fetch final merged content
    this.api.getResumeParsedContent(resumeId, traceParent).subscribe({
      next: (data) => {
        if (data) {
          this.progressiveParsedContent.set(data);
          this.defaultResumeParsedContent.set(data);
        }
      },
      error: () => {},
    });
  }

  /** Backward compat: called by old "ResumeParsed" SignalR message */
  onResumeParsed(resumeId: string, traceParent?: string): void {
    if (this.lastUploadedResumeId() !== resumeId) return;
    this.lastTraceParent = traceParent;

    this.api.getResumeParsedContent(resumeId, traceParent).subscribe({
      next: (data) => {
        if (data) {
          this.progressiveParsedContent.set(data);
          this.defaultResumeParsedContent.set(data);
          this.profileParseStatus.set('complete');
        }
      },
      error: () => this.profileParseStatus.set('error'),
    });
  }

  /** Called by ResumeRealtimeService when SignalR "ResumeEmbedded" arrives */
  onResumeEmbedded(_resumeId: string, traceParent?: string): void {
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
    if (!id || (status !== 'parsing' && status !== 'partial' && status !== 'retrying')) return;

    this.api.getResumeParsedContent(id).subscribe({
      next: (data) => {
        if (data) {
          this.progressiveParsedContent.set(data);
          this.defaultResumeParsedContent.set(data);
          // Check if we should consider it complete
          // (conservative: keep polling until AllSectionsCompleted arrives)
        }
      },
      error: () => this.profileParseStatus.set('error'),
    });
  }

  /** User confirms they want AI-parsed data applied to the profile form (backward compat) */
  applyParsedContent(): void {
    this.emitUserDecisionSpan('applied');
    const pending = this.pendingParsedContent();
    if (pending) {
      this.defaultResumeParsedContent.set(pending);
    }
    this.profileParseStatus.set('complete');
  }

  /** User declines AI auto-fill (backward compat) */
  dismissParsedContent(): void {
    this.emitUserDecisionSpan('dismissed');
    this.pendingParsedContent.set(null);
    this.profileParseStatus.set('idle');
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

  populateFromDefaultResume(): void {
    const defaultResume = this.resumes().find((r) => r.isDefault && r.hasParsedContent);
    if (!defaultResume) return;

    this.populatingFromResume.set(true);
    this.api.getResumeParsedContent(defaultResume.id).subscribe({
      next: (data) => {
        if (data) {
          this.progressiveParsedContent.set(data);
          this.defaultResumeParsedContent.set(data);
        }
        this.populatingFromResume.set(false);
      },
      error: () => this.populatingFromResume.set(false),
    });
  }

  reEmbedDefaultResume(): void {
    const defaultResume = this.resumes().find((r) => r.isDefault);
    if (!defaultResume) return;

    this.matchingJobsLoading.set(true);
    this.matchingJobsError.set(null);

    this.api.reEmbedResume(defaultResume.id).subscribe({
      next: () => {
        // Embedding will complete async — SignalR ResumeEmbedded handler will refresh matching jobs
      },
      error: (err) => {
        this.matchingJobsLoading.set(false);
        this.matchingJobsError.set(err?.error?.exceptions?.message ?? 'Failed to re-embed resume.');
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
