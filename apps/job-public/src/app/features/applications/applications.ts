import { Component, inject, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { ApplicationsListStore } from '../../core/stores/applications-list.store';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { EmptyState } from '../../shared/components/empty-state';

@Component({
  selector: 'app-applications',
  imports: [RouterLink, DatePipe, LoadingSpinner, EmptyState],
  template: `
    <div class="mx-auto max-w-7xl px-6 py-12">
      <div class="mb-8">
        <h1 class="text-3xl font-bold text-slate-900 dark:text-white">My Applications</h1>
        <p class="mt-2 text-sm text-slate-500 dark:text-slate-400">
          Track the status of your job applications.
        </p>
      </div>

      @if (store.loading()) {
        <app-loading-spinner label="Loading applications..." />
      } @else if (store.error()) {
        <div class="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700 dark:border-red-800 dark:bg-red-900/20 dark:text-red-400">
          {{ store.error() }}
        </div>
      } @else if (store.applications().length === 0) {
        <app-empty-state
          title="No applications yet"
          message="You haven't applied to any jobs yet. Browse open positions to get started."
        />
        <div class="mt-4 text-center">
          <a routerLink="/jobs" class="btn-primary">Browse Jobs</a>
        </div>
      } @else {
        <div class="space-y-4">
          @for (app of store.applications(); track app.id) {
            <div class="card flex items-center justify-between gap-4 p-5">
              <div class="min-w-0 flex-1">
                <a
                  [routerLink]="['/jobs', app.jobId]"
                  class="text-lg font-semibold text-slate-900 hover:text-primary-600 dark:text-white dark:hover:text-primary-400"
                >
                  {{ app.jobTitle }}
                </a>
                <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">
                  {{ app.companyName }}
                </p>
              </div>

              <div class="flex flex-shrink-0 items-center gap-4">
                <span [class]="statusClass(app.status)">
                  {{ formatStatus(app.status) }}
                </span>
                <span class="text-xs text-slate-400 dark:text-slate-500">
                  {{ app.createdAt | date: 'MMM d, y' }}
                </span>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class Applications implements OnInit {
  protected readonly store = inject(ApplicationsListStore);

  ngOnInit(): void {
    this.store.loadApplications();
  }

  statusClass(status: string): string {
    const base = 'inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium';
    switch (status) {
      case 'Submitted':
        return `${base} bg-blue-100 text-blue-700 dark:bg-blue-900/30 dark:text-blue-400`;
      case 'UnderReview':
        return `${base} bg-yellow-100 text-yellow-700 dark:bg-yellow-900/30 dark:text-yellow-400`;
      case 'Shortlisted':
        return `${base} bg-green-100 text-green-700 dark:bg-green-900/30 dark:text-green-400`;
      case 'Rejected':
        return `${base} bg-red-100 text-red-700 dark:bg-red-900/30 dark:text-red-400`;
      case 'Accepted':
        return `${base} bg-emerald-100 text-emerald-700 dark:bg-emerald-900/30 dark:text-emerald-400`;
      default:
        return `${base} bg-slate-100 text-slate-700 dark:bg-slate-700 dark:text-slate-300`;
    }
  }

  formatStatus(status: string): string {
    switch (status) {
      case 'UnderReview': return 'Under Review';
      default: return status;
    }
  }
}
