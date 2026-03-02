import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { JobTypeLabelPipe } from '../../../shared/pipes/job-type-label.pipe';
import { Job } from '../../../core/types/job.type';

@Component({
  selector: 'app-job-card',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe],
  template: `
    <a
      [routerLink]="['/jobs', job().id]"
      class="card-hover group flex items-center gap-4 border-l-4 border-l-primary-600 px-5 py-4"
    >
      <div
        class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary-100 text-sm font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-400"
      >
        {{ job().companyName.charAt(0) }}
      </div>

      <div class="min-w-0 flex-1">
        <h3
          class="truncate font-semibold text-slate-900 group-hover:text-primary-600 dark:text-white dark:group-hover:text-primary-400"
        >
          {{ job().title }}
        </h3>
        <div class="mt-0.5 flex flex-wrap items-center gap-x-2 gap-y-0.5 text-sm text-slate-500 dark:text-slate-400">
          <span>{{ job().companyName }}</span>
          <span class="text-slate-300 dark:text-slate-600">&middot;</span>
          <span>{{ job().location }}</span>
          <span class="text-slate-300 dark:text-slate-600">&middot;</span>
          <span
            class="rounded-full bg-slate-100 px-2 py-0.5 text-xs font-medium text-slate-700 dark:bg-slate-700 dark:text-slate-300"
          >
            {{ job().jobType | jobTypeLabel }}
          </span>
          <span class="text-slate-300 dark:text-slate-600">&middot;</span>
          <span class="font-medium text-slate-700 dark:text-slate-300">{{ job().salaryRange ?? 'Competitive' }}</span>
        </div>
      </div>

      <span class="shrink-0 text-xs text-slate-400 dark:text-slate-500">{{ job().createdAt | dateAgo }}</span>
    </a>
  `,
})
export class JobCard {
  job = input.required<Job>();
}
