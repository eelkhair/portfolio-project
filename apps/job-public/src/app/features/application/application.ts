import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ResumeUpload } from './components/resume-upload';
import { ApplicationForm } from './components/application-form';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { JobStore } from '../../core/stores/job.store';
import { ApplicationStore } from '../../core/stores/application.store';

@Component({
  selector: 'app-application',
  imports: [RouterLink, ResumeUpload, ApplicationForm, LoadingSpinner],
  template: `
    @if (jobStore.loading()) {
      <app-loading-spinner label="Loading job..." />
    } @else if (appStore.applicationStatus() === 'submitted') {
      <!-- Success state -->
      <div class="mx-auto max-w-2xl px-6 py-24 text-center">
        <div
          class="mx-auto flex h-20 w-20 items-center justify-center rounded-full bg-green-100 dark:bg-green-900/30"
        >
          <svg
            class="h-10 w-10 text-green-600 dark:text-green-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
          </svg>
        </div>
        <h1 class="mt-6 text-3xl font-bold text-slate-900 dark:text-white">Application Submitted!</h1>
        <p class="mt-3 text-slate-600 dark:text-slate-400">
          Your application has been sent successfully. The hiring team will review it and get back to you soon.
        </p>
        <div class="mt-8 flex items-center justify-center gap-4">
          <a routerLink="/jobs" class="btn-primary">Browse More Jobs</a>
          <a routerLink="/" class="btn-secondary">Go Home</a>
        </div>
      </div>
    } @else if (jobStore.currentJob(); as job) {
      <div class="mx-auto max-w-3xl px-6 py-12">
        <!-- Back link -->
        <a
          [routerLink]="['/jobs', job.id]"
          class="mb-6 inline-flex items-center gap-1 text-sm text-slate-500 hover:text-slate-900 dark:text-slate-400 dark:hover:text-white"
        >
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
          </svg>
          Back to job
        </a>

        <!-- Job summary -->
        <div class="card p-6">
          <div class="flex items-center gap-4">
            <div
              class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-primary-100 text-xl font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-400"
            >
              {{ job.companyName.charAt(0) }}
            </div>
            <div>
              <h1 class="text-xl font-bold text-slate-900 dark:text-white">
                Apply for {{ job.title }}
              </h1>
              <p class="text-sm text-slate-500 dark:text-slate-400">
                {{ job.companyName }} - {{ job.location }}
              </p>
            </div>
          </div>
        </div>

        <!-- Resume upload -->
        <div class="mt-8">
          <h2 class="section-heading text-lg">Step 1: Upload Resume</h2>
          <p class="mt-1 text-sm section-subheading">
            Upload your resume for AI-powered form auto-fill.
          </p>
          <div class="mt-4">
            <app-resume-upload />
          </div>
        </div>

        <!-- Application form -->
        <div class="mt-10">
          <h2 class="section-heading text-lg">Step 2: Complete Application</h2>
          <p class="mt-1 text-sm section-subheading">
            Review and complete your application details.
          </p>
          <div class="mt-4">
            <app-application-form [resumeData]="appStore.resumeData()" />
          </div>
        </div>
      </div>
    } @else {
      <div class="mx-auto max-w-7xl px-6 py-24 text-center">
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">Job not found</h1>
        <p class="mt-2 text-slate-500">This job listing may have been removed.</p>
        <a routerLink="/jobs" class="btn-primary mt-6 inline-flex">Browse all jobs</a>
      </div>
    }
  `,
})
export class Application implements OnInit, OnDestroy {
  protected readonly jobStore = inject(JobStore);
  protected readonly appStore = inject(ApplicationStore);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const jobId = this.route.snapshot.paramMap.get('jobId');
    if (jobId) {
      this.jobStore.loadJob(jobId);
    }
  }

  ngOnDestroy(): void {
    this.appStore.reset();
  }
}
