import { Component, computed, inject, OnInit } from '@angular/core';
import { JobCard } from './components/job-card';
import { JobSearchBar } from './components/job-search-bar';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { EmptyState } from '../../shared/components/empty-state';
import { JobStore } from '../../core/stores/job.store';
import { SearchStore } from '../../core/stores/search.store';

@Component({
  selector: 'app-jobs',
  imports: [JobCard, JobSearchBar, LoadingSpinner, EmptyState],
  template: `
    <div class="mx-auto max-w-7xl px-6 py-12">
      <div class="mb-8">
        <h1 class="section-heading text-3xl">Find Your Next Role</h1>
        <p class="mt-2 section-subheading">{{ resultsSummary() }}</p>
      </div>

      <div class="sticky top-0 z-10 pb-6">
        <app-job-search-bar />
      </div>

      @if (loading()) {
        <app-loading-spinner label="Searching jobs..." />
      } @else if (displayJobs().length === 0 && searchStore.hasSearched()) {
        <app-empty-state
          title="No jobs found"
          message="Try adjusting your search terms or clearing filters."
        />
      } @else {
        <div class="flex flex-col gap-[5px]">
          @for (job of displayJobs(); track job.id) {
            <app-job-card [job]="job" class="animate-fade-in" />
          }
        </div>

        @if (paginationTotalPages() > 1) {
          <div class="mt-8 flex items-center justify-center gap-3">
            <button
              (click)="goToPage(paginationCurrentPage() - 1)"
              [disabled]="!paginationHasPrev()"
              class="btn-secondary px-4 py-2 text-sm disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:transform-none"
            >
              Previous
            </button>

            <span class="text-sm text-slate-600">
              Page {{ paginationCurrentPage() }} of {{ paginationTotalPages() }}
            </span>

            <button
              (click)="goToPage(paginationCurrentPage() + 1)"
              [disabled]="!paginationHasNext()"
              class="btn-secondary px-4 py-2 text-sm disabled:opacity-40 disabled:cursor-not-allowed disabled:hover:transform-none"
            >
              Next
            </button>
          </div>
        }
      }
    </div>
  `,
})
export class Jobs implements OnInit {
  protected readonly jobStore = inject(JobStore);
  protected readonly searchStore = inject(SearchStore);

  protected readonly loading = computed(() => this.jobStore.loading() || this.searchStore.loading());

  protected readonly displayJobs = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.results() : this.jobStore.jobs(),
  );

  protected readonly paginationCurrentPage = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.currentPage() : this.jobStore.currentPage(),
  );
  protected readonly paginationTotalPages = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.totalPages() : this.jobStore.totalPages(),
  );
  protected readonly paginationHasPrev = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.hasPreviousPage() : this.jobStore.hasPreviousPage(),
  );
  protected readonly paginationHasNext = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.hasNextPage() : this.jobStore.hasNextPage(),
  );

  protected readonly resultsSummary = computed(() => {
    if (this.searchStore.hasSearched()) {
      const count = this.searchStore.totalCount();
      return `${count} result${count !== 1 ? 's' : ''} found`;
    }
    const total = this.jobStore.totalCount();
    return `Browse ${total} available position${total !== 1 ? 's' : ''}`;
  });

  ngOnInit(): void {
    this.jobStore.loadJobs();
  }

  goToPage(page: number): void {
    if (this.searchStore.hasSearched()) {
      this.searchStore.goToPage(page);
    } else {
      this.jobStore.loadJobs(page);
    }
  }
}
