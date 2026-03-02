import { inject, Injectable, signal } from '@angular/core';
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
}
