import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Company } from '../../../core/types/company.type';

@Component({
  selector: 'app-company-card',
  imports: [RouterLink],
  template: `
    <a
      [routerLink]="['/companies', company().id]"
      class="card-hover group block p-6"
    >
      <div class="flex items-start gap-4">
        <div
          class="flex h-14 w-14 shrink-0 items-center justify-center rounded-2xl bg-primary-100 text-xl font-bold text-primary-700 dark:bg-primary-900/30 dark:text-primary-400"
        >
          {{ company().name.charAt(0) }}
        </div>
        <div class="min-w-0">
          <h3
            class="text-lg font-semibold text-slate-900 group-hover:text-primary-600 dark:text-white dark:group-hover:text-primary-400"
          >
            {{ company().name }}
          </h3>
          <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">{{ company().industry }}</p>
        </div>
      </div>
      <p class="mt-4 line-clamp-2 text-sm text-slate-600 dark:text-slate-400">
        {{ company().description }}
      </p>
      <div class="mt-4 flex items-center gap-4 text-sm text-slate-500 dark:text-slate-400">
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
          {{ company().location }}
        </span>
        <span class="flex items-center gap-1">
          <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M15 19.128a9.38 9.38 0 002.625.372 9.337 9.337 0 004.121-.952 4.125 4.125 0 00-7.533-2.493M15 19.128v-.003c0-1.113-.285-2.16-.786-3.07M15 19.128v.106A12.318 12.318 0 018.624 21c-2.331 0-4.512-.645-6.374-1.766l-.001-.109a6.375 6.375 0 0111.964-3.07M12 6.375a3.375 3.375 0 11-6.75 0 3.375 3.375 0 016.75 0zm8.25 2.25a2.625 2.625 0 11-5.25 0 2.625 2.625 0 015.25 0z"
            />
          </svg>
          {{ company().size }}
        </span>
      </div>
    </a>
  `,
})
export class CompanyCard {
  company = input.required<Company>();
}
