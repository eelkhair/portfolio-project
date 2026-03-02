import { inject, Injectable, signal } from '@angular/core';
import mammoth from 'mammoth';
import { ApiService } from '../services/api.service';
import { ResumeResponse, UserProfile, UserProfileRequest } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ProfileStore {
  private readonly api = inject(ApiService);

  readonly profile = signal<UserProfile | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly saved = signal(false);
  readonly error = signal<string | null>(null);

  // Resume state
  readonly resumes = signal<ResumeResponse[]>([]);
  readonly uploading = signal(false);
  readonly uploadError = signal<string | null>(null);

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

  loadResumes(): void {
    this.api.getResumes().subscribe({
      next: (resumes) => this.resumes.set(resumes),
      error: () => {}, // Silently fail — resumes may not exist yet
    });
  }

  uploadResume(file: File): void {
    this.uploading.set(true);
    this.uploadError.set(null);

    this.api.uploadResume(file).subscribe({
      next: (resume) => {
        this.resumes.update((list) => [resume, ...list]);
        this.uploading.set(false);
      },
      error: (err) => {
        this.uploading.set(false);
        this.uploadError.set(err?.error?.exceptions?.message ?? 'Failed to upload resume.');
      },
    });
  }

  deleteResume(id: string): void {
    this.api.deleteResume(id).subscribe({
      next: () => {
        this.resumes.update((list) => list.filter((r) => r.id !== id));
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
}
