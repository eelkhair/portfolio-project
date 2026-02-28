import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { Job } from '../../../core/types/job.type';

@Component({
  selector: 'app-similar-jobs',
  imports: [RouterLink, DateAgoPipe],
  template: `
    @if (jobs().length > 0) {
      <div>
        <h3 class="section-heading text-lg">Similar Jobs</h3>
        <div class="mt-4 space-y-3">
          @for (job of jobs(); track job.id) {
            <a
              [routerLink]="['/jobs', job.id]"
              class="card-hover block p-4"
            >
              <h4
                class="font-medium text-slate-900 hover:text-primary-600 dark:text-white dark:hover:text-primary-400"
              >
                {{ job.title }}
              </h4>
              <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">{{ job.companyName }}</p>
              <div class="mt-2 flex items-center gap-2 text-xs text-slate-400">
                <span>{{ job.jobType }}</span>
                <span>-</span>
                <span>{{ job.createdAt | dateAgo }}</span>
              </div>
            </a>
          }
        </div>
      </div>
    }
  `,
})
export class SimilarJobs {
  jobs = input.required<Job[]>();
}
