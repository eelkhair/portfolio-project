import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobCard } from '../jobs/components/job-card';
import { JobStore } from '../../core/stores/job.store';
import { MockDataService } from '../../core/services/mock-data.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, JobCard],
  template: `
    <!-- Hero -->
    <section class="relative overflow-hidden bg-slate-900 text-white">
      <div class="absolute inset-0 opacity-20">
        <div
          class="absolute -top-40 right-0 h-96 w-96 rounded-full bg-primary-500 blur-3xl"
        ></div>
        <div
          class="absolute -bottom-20 left-0 h-80 w-80 rounded-full bg-accent-500 blur-3xl"
        ></div>
      </div>
      <div class="relative mx-auto max-w-7xl px-6 py-24 text-center md:py-32">
        <h1 class="text-5xl font-bold tracking-tight md:text-6xl">
          Find your next
          <span class="ai-gradient-text">opportunity</span>
        </h1>
        <p class="mx-auto mt-6 max-w-2xl text-lg text-slate-300">
          Browse curated jobs from top tech companies. AI-powered resume parsing
          and smart matching to accelerate your job search.
        </p>
        <div class="mt-10 flex flex-col items-center justify-center gap-4 sm:flex-row">
          <a routerLink="/jobs" class="btn-primary text-base">
            Browse Jobs
            <svg
              class="ml-2 h-5 w-5"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              stroke-width="2"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3"
              />
            </svg>
          </a>
          <a routerLink="/companies" class="btn-secondary">View Companies</a>
        </div>
      </div>
    </section>

    <!-- Stats -->
    <section class="border-b border-slate-200 bg-white dark:border-slate-700 dark:bg-slate-800">
      <div class="mx-auto max-w-7xl px-6 py-10">
        <div class="grid grid-cols-1 gap-8 sm:grid-cols-3">
          <div class="text-center">
            <div class="text-3xl font-bold text-primary-600">{{ jobCount }}</div>
            <div class="mt-1 text-sm text-slate-500 dark:text-slate-400">Open Positions</div>
          </div>
          <div class="text-center">
            <div class="text-3xl font-bold text-primary-600">{{ companyCount }}</div>
            <div class="mt-1 text-sm text-slate-500 dark:text-slate-400">Hiring Companies</div>
          </div>
          <div class="text-center">
            <div class="text-3xl font-bold text-primary-600">2.4K+</div>
            <div class="mt-1 text-sm text-slate-500 dark:text-slate-400">Applications Submitted</div>
          </div>
        </div>
      </div>
    </section>

    <!-- Featured Jobs -->
    <section class="mx-auto max-w-7xl px-6 py-16">
      <div class="flex items-center justify-between">
        <div>
          <h2 class="section-heading text-3xl">Featured Jobs</h2>
          <p class="mt-2 section-subheading">Hand-picked opportunities from top companies.</p>
        </div>
        <a
          routerLink="/jobs"
          class="hidden text-sm font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400 sm:inline-flex sm:items-center"
        >
          View all jobs
          <svg
            class="ml-1 h-4 w-4"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3"
            />
          </svg>
        </a>
      </div>
      <div class="mt-8 grid grid-cols-1 gap-4 lg:grid-cols-2">
        @for (job of store.featuredJobs(); track job.id) {
          <app-job-card [job]="job" class="animate-fade-in" />
        }
      </div>
      <div class="mt-8 text-center sm:hidden">
        <a routerLink="/jobs" class="btn-primary">View All Jobs</a>
      </div>
    </section>

    <!-- AI CTA -->
    <section class="bg-slate-50 dark:bg-slate-900">
      <div class="mx-auto max-w-7xl px-6 py-16">
        <div class="card overflow-hidden">
          <div class="ai-gradient p-px rounded-xl">
            <div class="rounded-xl bg-white p-10 text-center dark:bg-slate-800 md:p-16">
              <div
                class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl ai-gradient"
              >
                <svg
                  class="h-7 w-7 text-white"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09zM18.259 8.715L18 9.75l-.259-1.035a3.375 3.375 0 00-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 002.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 002.455 2.456L21.75 6l-1.036.259a3.375 3.375 0 00-2.455 2.456z"
                  />
                </svg>
              </div>
              <h2 class="mt-6 text-2xl font-bold text-slate-900 dark:text-white">
                AI-Powered Applications
              </h2>
              <p class="mx-auto mt-3 max-w-lg text-slate-600 dark:text-slate-400">
                Upload your resume and let AI auto-fill your application. Smart parsing extracts your
                experience, skills, and contact info instantly.
              </p>
              <a routerLink="/jobs" class="btn-accent mt-8 inline-flex">
                Get Started
              </a>
            </div>
          </div>
        </div>
      </div>
    </section>
  `,
})
export class Home implements OnInit {
  protected readonly store = inject(JobStore);
  private readonly dataService = inject(MockDataService);

  readonly jobCount = this.dataService.getJobCount();
  readonly companyCount = this.dataService.getCompanyCount();

  ngOnInit(): void {
    this.store.loadFeaturedJobs();
  }
}
