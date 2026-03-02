import { Component, effect, inject, OnInit } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { ProfileStore } from '../../core/stores/profile.store';
import { AccountService } from '../../core/services/account.service';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinner, DatePipe],
  template: `
    @if (store.loading()) {
      <app-loading-spinner label="Loading profile..." />
    } @else {
      <div class="mx-auto max-w-3xl px-6 py-12">
        <!-- Back link -->
        <a
          routerLink="/jobs"
          class="mb-6 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
          Back to jobs
        </a>

        <!-- Page heading -->
        <div class="mb-8">
          <h1 class="section-heading text-2xl">Your Profile</h1>
          <p class="mt-1 text-sm section-subheading">
            Manage your profile information and job preferences.
          </p>
        </div>

        <!-- Personal Info (read-only) -->
        <div class="card p-6">
          <h2 class="text-base font-semibold text-slate-900 dark:text-white">Personal Info</h2>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">From your sign-in account.</p>
          <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
            <div>
              <span class="block text-xs font-medium text-slate-500 dark:text-slate-400">Name</span>
              <span class="mt-0.5 block text-sm font-medium text-slate-900 dark:text-white">
                {{ account.displayName() }}
              </span>
            </div>
            <div>
              <span class="block text-xs font-medium text-slate-500 dark:text-slate-400">Email</span>
              <span class="mt-0.5 block text-sm font-medium text-slate-900 dark:text-white">
                {{ store.profile()?.email || account.user()?.email || '—' }}
              </span>
            </div>
          </div>
        </div>

        <!-- Resume Section -->
        <div class="card mt-6 p-6">
          <h2 class="text-base font-semibold text-slate-900 dark:text-white">Resumes</h2>
          <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
            Upload your resume to use when applying for jobs. PDF, DOCX, or TXT (max 5 MB).
          </p>

          <!-- Drop zone -->
          <div
            class="mt-4 flex cursor-pointer flex-col items-center justify-center rounded-lg border-2 border-dashed border-slate-300 px-6 py-8 transition hover:border-primary-400 hover:bg-primary-50/50 dark:border-slate-600 dark:hover:border-primary-500 dark:hover:bg-primary-900/10"
            (click)="fileInput.click()"
            (dragover)="$event.preventDefault()"
            (drop)="onFileDrop($event)"
          >
            @if (store.uploading()) {
              <div class="h-8 w-8 rounded-full border-2 border-primary-200 border-t-primary-600 animate-spin"></div>
              <p class="mt-3 text-sm text-slate-600 dark:text-slate-400">Uploading...</p>
            } @else {
              <svg class="h-10 w-10 text-slate-400 dark:text-slate-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5" />
              </svg>
              <p class="mt-2 text-sm font-medium text-slate-700 dark:text-slate-300">
                Drop a file here or <span class="text-primary-600 dark:text-primary-400">browse</span>
              </p>
              <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">PDF, DOCX, or TXT up to 5 MB</p>
            }
            <input
              #fileInput
              type="file"
              class="hidden"
              accept=".pdf,.docx,.txt,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document,text/plain"
              (change)="onFileSelected($event)"
            />
          </div>

          <!-- Upload error -->
          @if (store.uploadError()) {
            <div class="mt-3 rounded-lg bg-red-50 px-4 py-2.5 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-400">
              {{ store.uploadError() }}
            </div>
          }

          <!-- Resume list -->
          @if (store.resumes().length > 0) {
            <ul class="mt-4 divide-y divide-slate-200 dark:divide-slate-700">
              @for (resume of store.resumes(); track resume.id) {
                <li class="flex items-center justify-between py-3">
                  <div class="flex items-center gap-3">
                    <div class="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-100 dark:bg-slate-700">
                      <svg class="h-5 w-5 text-slate-500 dark:text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.5">
                        <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                      </svg>
                    </div>
                    <div>
                      <p class="text-sm font-medium text-slate-900 dark:text-white">{{ resume.originalFileName }}</p>
                      <p class="text-xs text-slate-500 dark:text-slate-400">
                        {{ resume.createdAt | date:'mediumDate' }}
                        @if (resume.fileSize) {
                          <span class="mx-1">·</span>{{ formatFileSize(resume.fileSize) }}
                        }
                      </p>
                    </div>
                  </div>
                  <button
                    type="button"
                    (click)="onDeleteResume(resume.id)"
                    class="rounded p-1.5 text-slate-400 transition hover:bg-red-50 hover:text-red-600 dark:hover:bg-red-900/20 dark:hover:text-red-400"
                    title="Delete resume"
                  >
                    <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                      <path stroke-linecap="round" stroke-linejoin="round" d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
                    </svg>
                  </button>
                </li>
              }
            </ul>
          }
        </div>

        <form [formGroup]="form" (ngSubmit)="onSave()" class="mt-6 space-y-6">
          <!-- Contact & Links -->
          <div class="card p-6">
            <h2 class="text-base font-semibold text-slate-900 dark:text-white">Contact & Links</h2>
            <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Phone</label>
                <input type="tel" formControlName="phone" class="input-field" placeholder="+1 (555) 000-0000" />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">LinkedIn</label>
                <input type="text" formControlName="linkedin" class="input-field" placeholder="linkedin.com/in/you" />
              </div>
              <div class="sm:col-span-2">
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Portfolio</label>
                <input type="text" formControlName="portfolio" class="input-field" placeholder="yoursite.com" />
              </div>
            </div>
          </div>

          <!-- Skills & Experience -->
          <div class="card p-6">
            <h2 class="text-base font-semibold text-slate-900 dark:text-white">Skills & Experience</h2>
            <div class="mt-4 space-y-4">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Key Skills</label>
                <input type="text" formControlName="skills" class="input-field" placeholder="TypeScript, React, Node.js, AWS" />
                <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">Comma-separated list of your top skills.</p>
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Experience Summary</label>
                <textarea
                  formControlName="experience"
                  rows="4"
                  class="input-field"
                  placeholder="Brief summary of your professional experience..."
                ></textarea>
              </div>
            </div>
          </div>

          <!-- Job Preferences -->
          <div class="card p-6">
            <h2 class="text-base font-semibold text-slate-900 dark:text-white">Job Preferences</h2>
            <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">Used for daily job matching recommendations.</p>
            <div class="mt-4 grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Preferred Location</label>
                <input type="text" formControlName="preferredLocation" class="input-field" placeholder="Portland, OR" />
              </div>
              <div>
                <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">Preferred Job Type</label>
                <select formControlName="preferredJobType" class="input-field">
                  <option value="">No preference</option>
                  <option value="fullTime">Full Time</option>
                  <option value="partTime">Part Time</option>
                  <option value="contract">Contract</option>
                  <option value="internship">Internship</option>
                </select>
              </div>
            </div>
          </div>

          <!-- Error -->
          @if (store.error()) {
            <div class="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700 dark:bg-red-900/20 dark:text-red-400">
              {{ store.error() }}
            </div>
          }

          <!-- Actions -->
          <div class="flex items-center gap-4">
            <button
              type="submit"
              [disabled]="store.saving()"
              class="btn-primary disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:translate-y-0"
            >
              @if (store.saving()) {
                <div class="mr-2 h-4 w-4 rounded-full border-2 border-white/30 border-t-white animate-spin"></div>
                Saving...
              } @else {
                Save Profile
              }
            </button>

            @if (store.saved()) {
              <span class="flex items-center gap-1.5 text-sm font-medium text-green-600 dark:text-green-400">
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                </svg>
                Profile saved
              </span>
            }
          </div>
        </form>
      </div>
    }
  `,
})
export class Profile implements OnInit {
  protected readonly store = inject(ProfileStore);
  protected readonly account = inject(AccountService);
  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.group({
    phone: [''],
    linkedin: [''],
    portfolio: [''],
    skills: [''],
    experience: [''],
    preferredLocation: [''],
    preferredJobType: [''],
  });

  constructor() {
    // Pre-fill form when profile loads
    effect(() => {
      const profile = this.store.profile();
      if (profile) {
        this.form.patchValue({
          phone: profile.phone ?? '',
          linkedin: profile.linkedin ?? '',
          portfolio: profile.portfolio ?? '',
          skills: profile.skills.join(', '),
          experience: profile.experience ?? '',
          preferredLocation: profile.preferredLocation ?? '',
          preferredJobType: profile.preferredJobType ?? '',
        });
      }
    });
  }

  ngOnInit(): void {
    this.store.loadProfile();
    this.store.loadResumes();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.store.uploadResume(input.files[0]);
      input.value = '';
    }
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.store.uploadResume(file);
    }
  }

  onDeleteResume(id: string): void {
    this.store.deleteResume(id);
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  onSave(): void {
    const val = this.form.value;

    this.store.saveProfile({
      phone: val.phone || undefined,
      linkedin: val.linkedin || undefined,
      portfolio: val.portfolio || undefined,
      experience: val.experience || undefined,
      skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
      preferredLocation: val.preferredLocation || undefined,
      preferredJobType: val.preferredJobType || undefined,
    });
  }
}
