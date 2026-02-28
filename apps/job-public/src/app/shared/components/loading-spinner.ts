import { Component, input } from '@angular/core';

@Component({
  selector: 'app-loading-spinner',
  template: `
    <div class="flex flex-col items-center justify-center py-12" [attr.aria-label]="label()">
      <div
        class="h-10 w-10 rounded-full border-4 border-slate-200 border-t-primary-600 animate-spin dark:border-slate-700"
      ></div>
      @if (label()) {
        <p class="mt-4 text-sm text-slate-500 dark:text-slate-400">{{ label() }}</p>
      }
    </div>
  `,
})
export class LoadingSpinner {
  label = input('Loading...');
}
