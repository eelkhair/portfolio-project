import { inject, Injectable, signal } from '@angular/core';
import { of, switchMap } from 'rxjs';
import { catchError, delay } from 'rxjs/operators';
import { ApiService } from '../services/api.service';
import { MockDataService } from '../services/mock-data.service';
import { ApplicationStatus, SubmitApplicationRequest } from '../types/application.type';
import { ParseStatus, ResumeData, UserProfile, UserProfileRequest } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ApplicationStore {
  private readonly api = inject(ApiService);
  private readonly mockData = inject(MockDataService);

  readonly parseStatus = signal<ParseStatus>('idle');
  readonly resumeData = signal<ResumeData | null>(null);
  readonly applicationStatus = signal<ApplicationStatus>('idle');
  readonly fileName = signal('');
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

    // TODO: Replace with real AI resume parsing endpoint
    of(null)
      .pipe(delay(800))
      .subscribe(() => {
        this.parseStatus.set('parsing');

        this.mockData.parseResume().subscribe((data) => {
          this.resumeData.set(data);
          this.parseStatus.set('parsed');
        });
      });
  }

  submitApplication(jobId: string, coverLetter: string, profileData: UserProfileRequest, resumeId?: string): void {
    this.applicationStatus.set('submitting');
    this.error.set(null);

    const appRequest: SubmitApplicationRequest = {
      jobId,
      resumeId: resumeId || undefined,
      coverLetter: coverLetter || undefined,
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

  reset(): void {
    this.parseStatus.set('idle');
    this.resumeData.set(null);
    this.applicationStatus.set('idle');
    this.fileName.set('');
    this.profileLoaded.set(false);
    this.error.set(null);
  }
}
