import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { Job } from '../../../core/types/job.type';

@Component({
  selector: 'app-job-card',
  imports: [RouterLink, DateAgoPipe],
  template: `
    <a
      [routerLink]="['/jobs', job().id]"
      class="card-hover group block border-l-4 border-l-primary-600 p-6"
    >
      <div class="flex items-start justify-between">
        <div class="flex items-start gap-4">
          <div
            class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-primary-100 text-lg font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-400"
          >
            {{ job().companyName.charAt(0) }}
          </div>
          <div>
            <h3
              class="text-lg font-semibold text-slate-900 group-hover:text-primary-600 dark:text-white dark:group-hover:text-primary-400"
            >
              {{ job().title }}
            </h3>
            <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">{{ job().companyName }}</p>
          </div>
        </div>
      </div>

      <div class="mt-4 flex flex-wrap items-center gap-3 text-sm text-slate-500 dark:text-slate-400">
        <span class="flex items-center gap-1">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M15 10.5a3 3 0 11-6 0 3 3 0 016 0z"
            />
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M19.5 10.5c0 7.142-7.5 11.25-7.5 11.25S4.5 17.642 4.5 10.5a7.5 7.5 0 0115 0z"
            />
          </svg>
          {{ job().location }}
        </span>
        <span
          class="rounded-full bg-slate-100 px-2.5 py-0.5 text-xs font-medium text-slate-700 dark:bg-slate-700 dark:text-slate-300"
        >
          {{ job().jobType }}
        </span>
        <span class="font-medium text-slate-700 dark:text-slate-300">{{ job().salaryRange ?? 'Competitive' }}</span>
      </div>

      <div class="mt-3 flex items-center justify-end">
        <span class="text-xs text-slate-400 dark:text-slate-500">{{ job().createdAt | dateAgo }}</span>
      </div>
    </a>
  `,
})
export class JobCard {
  job = input.required<Job>();
}
