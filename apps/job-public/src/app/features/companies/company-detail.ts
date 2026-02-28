import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { JobCard } from '../jobs/components/job-card';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { CompanyStore } from '../../core/stores/company.store';

@Component({
  selector: 'app-company-detail',
  imports: [RouterLink, JobCard, LoadingSpinner, DatePipe],
  template: `
    @if (store.loading()) {
      <app-loading-spinner label="Loading company..." />
    } @else if (store.currentCompany(); as company) {
      <!-- Header -->
      <div class="bg-slate-900 text-white">
        <div class="mx-auto max-w-7xl px-6 py-12">
          <a
            routerLink="/companies"
            class="mb-6 inline-flex items-center gap-1 text-sm text-slate-400 hover:text-white"
          >
            <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M10.5 19.5L3 12m0 0l7.5-7.5M3 12h18" />
            </svg>
            Back to companies
          </a>
          <div class="flex items-center gap-5">
            <div
              class="flex h-20 w-20 shrink-0 items-center justify-center rounded-2xl bg-primary-600 text-3xl font-bold"
            >
              {{ company.name.charAt(0) }}
            </div>
            <div>
              <h1 class="text-3xl font-bold">{{ company.name }}</h1>
              <div class="mt-2 flex flex-wrap items-center gap-3 text-slate-300">
                <span>{{ company.industryName }}</span>
                <span class="text-slate-600">|</span>
                <span>{{ company.size }} employees</span>
                <span class="text-slate-600">|</span>
                <span>Founded {{ company.founded ? (company.founded | date:'yyyy') : 'N/A' }}</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Content -->
      <div class="mx-auto max-w-7xl px-6 py-10">
        <div class="grid grid-cols-1 gap-10 lg:grid-cols-3">
          <!-- About -->
          <div class="lg:col-span-2">
            <h2 class="section-heading">About {{ company.name }}</h2>
            <p class="mt-4 leading-relaxed text-slate-600 dark:text-slate-400">
              {{ company.about }}
            </p>
          </div>

          <!-- Info sidebar -->
          <div class="card p-6 h-fit">
            <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-400">Company Info</h3>
            <dl class="mt-4 space-y-3">
              <div>
                <dt class="text-xs text-slate-500 dark:text-slate-400">Industry</dt>
                <dd class="font-medium text-slate-900 dark:text-white">{{ company.industryName }}</dd>
              </div>
              <div>
                <dt class="text-xs text-slate-500 dark:text-slate-400">Company Size</dt>
                <dd class="font-medium text-slate-900 dark:text-white">{{ company.size }}</dd>
              </div>
              <div>
                <dt class="text-xs text-slate-500 dark:text-slate-400">Founded</dt>
                <dd class="font-medium text-slate-900 dark:text-white">{{ company.founded ? (company.founded | date:'yyyy') : 'N/A' }}</dd>
              </div>
            </dl>
          </div>
        </div>

        <!-- Open positions -->
        <div class="mt-12">
          <h2 class="section-heading">
            Open Positions
            <span class="ml-2 text-lg font-normal text-slate-400">
              ({{ store.companyJobs().length }})
            </span>
          </h2>
          <div class="mt-6 space-y-4">
            @for (job of store.companyJobs(); track job.id) {
              <app-job-card [job]="job" />
            }
            @empty {
              <p class="py-8 text-center text-slate-500 dark:text-slate-400">
                No open positions at the moment.
              </p>
            }
          </div>
        </div>
      </div>
    } @else {
      <div class="mx-auto max-w-7xl px-6 py-24 text-center">
        <h1 class="text-2xl font-bold text-slate-900 dark:text-white">Company not found</h1>
        <p class="mt-2 text-slate-500">This company profile may have been removed.</p>
        <a routerLink="/companies" class="btn-primary mt-6 inline-flex">Browse companies</a>
      </div>
    }
  `,
})
export class CompanyDetail implements OnInit {
  protected readonly store = inject(CompanyStore);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.store.loadCompany(id);
    }
  }
}
