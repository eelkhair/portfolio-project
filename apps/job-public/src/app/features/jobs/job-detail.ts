import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../shared/pipes/date-ago.pipe';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { SimilarJobs } from './components/similar-jobs';
import { JobStore } from '../../core/stores/job.store';

@Component({
  selector: 'app-job-detail',
  imports: [RouterLink, DateAgoPipe, LoadingSpinner, SimilarJobs],
  template: `
    @if (store.loading()) {
      <app-loading-spinner label="Loading job details..." />
    } @else if (store.currentJob(); as job) {
      <!-- Hero -->
      <div class="bg-slate-900 text-white">
        <div class="mx-auto max-w-7xl px-6 py-12">
          <a routerLink="/jobs" class="mb-6 inline-flex items-center gap-1 text-sm text-slate-400 hover:text-white">
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
            </svg>
            Back to jobs
          </a>
          <div class="flex items-start justify-between">
            <div class="flex items-start gap-5">
              <div
                class="flex h-16 w-16 shrink-0 items-center justify-center rounded-2xl bg-primary-600 text-2xl font-bold"
              >
                {{ job.companyName.charAt(0) }}
              </div>
              <div>
                <h1 class="text-3xl font-bold">{{ job.title }}</h1>
                <div class="mt-2 flex flex-wrap items-center gap-3 text-slate-300">
                  <a
                    [routerLink]="['/companies', job.companyId]"
                    class="font-medium hover:text-white"
                  >
                    {{ job.companyName }}
                  </a>
                  <span class="text-slate-600">|</span>
                  <span>{{ job.location }}</span>
                  <span class="text-slate-600">|</span>
                  <span>{{ job.type }}</span>
                </div>
                <div class="mt-3 flex items-center gap-4">
                  <span class="text-lg font-semibold text-accent-400">{{ job.salary }}</span>
                  <span class="text-sm text-slate-400">Posted {{ job.postedAt | dateAgo }}</span>
                </div>
              </div>
            </div>
            <a
              [routerLink]="['/apply', job.id]"
              class="btn-primary hidden md:inline-flex"
            >
              Apply Now
            </a>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="mx-auto max-w-7xl px-6 py-10">
        <!-- Mobile apply button -->
        <a [routerLink]="['/apply', job.id]" class="btn-primary mb-8 w-full md:hidden">
          Apply Now
        </a>

        <div class="grid grid-cols-1 gap-10 lg:grid-cols-3">
          <!-- Main content -->
          <div class="lg:col-span-2 space-y-8">
            <section>
              <h2 class="section-heading">About this role</h2>
              <p class="mt-3 leading-relaxed text-slate-600 dark:text-slate-400">
                {{ job.description }}
              </p>
            </section>

            <section>
              <h2 class="section-heading">Responsibilities</h2>
              <ul class="mt-3 space-y-2">
                @for (item of job.responsibilities; track item) {
                  <li class="flex items-start gap-3 text-slate-600 dark:text-slate-400">
                    <svg
                      class="mt-1 h-5 w-5 shrink-0 text-primary-500"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-width="2"
                    >
                      <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                    </svg>
                    {{ item }}
                  </li>
                }
              </ul>
            </section>

            <section>
              <h2 class="section-heading">Qualifications</h2>
              <ul class="mt-3 space-y-2">
                @for (item of job.qualifications; track item) {
                  <li class="flex items-start gap-3 text-slate-600 dark:text-slate-400">
                    <svg
                      class="mt-1 h-5 w-5 shrink-0 text-slate-400"
                      fill="none"
                      viewBox="0 0 24 24"
                      stroke="currentColor"
                      stroke-width="2"
                    >
                      <path
                        stroke-linecap="round"
                        stroke-linejoin="round"
                        d="M9 12.75L11.25 15 15 9.75M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                      />
                    </svg>
                    {{ item }}
                  </li>
                }
              </ul>
            </section>

            <section>
              <h2 class="section-heading">Skills</h2>
              <div class="mt-3 flex flex-wrap gap-2">
                @for (skill of job.skills; track skill) {
                  <span
                    class="rounded-lg bg-primary-50 px-3 py-1.5 text-sm font-medium text-primary-700 dark:bg-primary-900/20 dark:text-primary-400"
                  >
                    {{ skill }}
                  </span>
                }
              </div>
            </section>
          </div>

          <!-- Sidebar -->
          <aside class="space-y-6">
            <!-- Company card -->
            <div class="card p-6">
              <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-400">
                About the company
              </h3>
              <div class="mt-4 flex items-center gap-3">
                <div
                  class="flex h-12 w-12 items-center justify-center rounded-xl bg-primary-100 text-lg font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-400"
                >
                  {{ job.companyName.charAt(0) }}
                </div>
                <div>
                  <a
                    [routerLink]="['/companies', job.companyId]"
                    class="font-semibold text-slate-900 hover:text-primary-600 dark:text-white dark:hover:text-primary-400"
                  >
                    {{ job.companyName }}
                  </a>
                </div>
              </div>
              <a
                [routerLink]="['/companies', job.companyId]"
                class="mt-4 inline-flex text-sm font-medium text-primary-600 hover:text-primary-700 dark:text-primary-400"
              >
                View company profile
                <svg class="ml-1 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M13.5 4.5L21 12m0 0l-7.5 7.5M21 12H3" />
                </svg>
              </a>
            </div>

            <!-- Job details -->
            <div class="card p-6">
              <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-400">
                Job Details
              </h3>
              <dl class="mt-4 space-y-3">
                <div>
                  <dt class="text-xs text-slate-500 dark:text-slate-400">Experience Level</dt>
                  <dd class="font-medium text-slate-900 dark:text-white">{{ job.experienceLevel }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-slate-500 dark:text-slate-400">Job Type</dt>
                  <dd class="font-medium text-slate-900 dark:text-white">{{ job.type }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-slate-500 dark:text-slate-400">Location</dt>
                  <dd class="font-medium text-slate-900 dark:text-white">{{ job.location }}</dd>
                </div>
                <div>
                  <dt class="text-xs text-slate-500 dark:text-slate-400">Salary</dt>
                  <dd class="font-medium text-slate-900 dark:text-white">{{ job.salary }}</dd>
                </div>
              </dl>
            </div>

            <app-similar-jobs [jobs]="store.similarJobs()" />
          </aside>
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
export class JobDetail implements OnInit {
  protected readonly store = inject(JobStore);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.store.loadJob(id);
    }
  }
}
