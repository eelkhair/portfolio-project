import { Component, HostListener, input, output } from '@angular/core';

@Component({
  selector: 'app-confirm-dialog',
  template: `
    @if (open()) {
      <div
        class="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm animate-fade-in"
        (click)="onBackdropClick($event)"
      >
        <div
          class="mx-4 w-full max-w-md rounded-xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-700 dark:bg-slate-800"
          (click)="$event.stopPropagation()"
        >
          <!-- Icon -->
          <div class="flex h-12 w-12 items-center justify-center rounded-full" [class]="iconBgClass()">
            @if (variant() === 'danger') {
              <svg class="h-6 w-6 text-red-600 dark:text-red-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z" />
              </svg>
            } @else {
              <svg class="h-6 w-6 text-primary-600 dark:text-primary-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z" />
              </svg>
            }
          </div>

          <!-- Text -->
          <h3 class="mt-4 text-base font-semibold text-slate-900 dark:text-white">{{ title() }}</h3>
          <p class="mt-1.5 text-sm text-slate-500 dark:text-slate-400">{{ message() }}</p>

          <!-- Actions -->
          <div class="mt-6 flex items-center justify-end gap-3">
            <button type="button" (click)="onCancel()" class="btn-secondary">
              {{ cancelLabel() }}
            </button>
            <button
              type="button"
              (click)="onConfirm()"
              class="inline-flex items-center justify-center rounded-lg px-4 py-2.5 text-sm font-semibold text-white shadow-xs transition hover:-translate-y-0.5 hover:shadow-lg"
              [class]="confirmBtnClass()"
            >
              {{ confirmLabel() }}
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class ConfirmDialog {
  open = input(false);
  title = input('Are you sure?');
  message = input('This action cannot be undone.');
  confirmLabel = input('Confirm');
  cancelLabel = input('Cancel');
  variant = input<'danger' | 'default'>('default');

  confirmed = output<void>();
  cancelled = output<void>();

  iconBgClass(): string {
    return this.variant() === 'danger'
      ? 'bg-red-100 dark:bg-red-900/30'
      : 'bg-primary-100 dark:bg-primary-900/30';
  }

  confirmBtnClass(): string {
    return this.variant() === 'danger'
      ? 'bg-red-600 hover:bg-red-700 focus:ring-red-500'
      : 'bg-primary-600 hover:bg-primary-700 focus:ring-primary-500';
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.open()) {
      this.onCancel();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onCancel();
    }
  }

  onConfirm(): void {
    this.confirmed.emit();
  }

  onCancel(): void {
    this.cancelled.emit();
  }
}
