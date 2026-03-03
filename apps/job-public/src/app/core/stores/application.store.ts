import { inject, Injectable, signal } from '@angular/core';
import { switchMap } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { ApiService } from '../services/api.service';
import { ApplicationStatus, PersonalInfoDto, WorkHistoryDto, EducationDto, CertificationDto, SubmitApplicationRequest } from '../types/application.type';
import { ParseStatus, ResumeData, UserProfile, UserProfileRequest } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ApplicationStore {
  private readonly api = inject(ApiService);

  readonly parseStatus = signal<ParseStatus>('idle');
  readonly resumeData = signal<ResumeData | null>(null);
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

    this.api.uploadResume(file).subscribe({
      next: (resume) => {
        this.resumeId.set(resume.id);
        if (resume.parsedContent) {
          this.resumeData.set(resume.parsedContent);
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
    this.resumeId.set(null);
    this.fileName.set('');
  }

  reset(): void {
    this.resetParse();
    this.applicationStatus.set('idle');
    this.profileLoaded.set(false);
    this.error.set(null);
  }
}
