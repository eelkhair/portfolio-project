import { Component, input } from '@angular/core';

@Component({
  selector: 'app-empty-state',
  template: `
    <div class="flex flex-col items-center justify-center py-16 text-center">
      <div
        class="flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 dark:bg-slate-700"
      >
        <svg
          class="h-8 w-8 text-slate-400"
          fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
          stroke-width="1.5"
        >
          <path
            stroke-linecap="round"
            stroke-linejoin="round"
            d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z"
          />
        </svg>
      </div>
      <h3 class="mt-4 text-lg font-semibold text-slate-900 dark:text-white">{{ title() }}</h3>
      <p class="mt-2 max-w-sm text-sm text-slate-500 dark:text-slate-400">{{ message() }}</p>
    </div>
  `,
})
export class EmptyState {
  title = input('No results found');
  message = input('Try adjusting your search or filters to find what you are looking for.');
}
