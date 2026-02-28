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
        <div class="space-y-4">
          @for (job of displayJobs(); track job.id) {
            <app-job-card [job]="job" class="animate-fade-in" />
          }
        </div>
      }
    </div>
  `,
})
export class Jobs implements OnInit {
  private readonly jobStore = inject(JobStore);
  protected readonly searchStore = inject(SearchStore);

  protected readonly loading = computed(() => this.jobStore.loading() || this.searchStore.loading());

  protected readonly displayJobs = computed(() =>
    this.searchStore.hasSearched() ? this.searchStore.results() : this.jobStore.jobs(),
  );

  protected readonly resultsSummary = computed(() => {
    const count = this.displayJobs().length;
    if (this.searchStore.hasSearched()) {
      return `${count} result${count !== 1 ? 's' : ''} found`;
    }
    return `Browse ${count} available position${count !== 1 ? 's' : ''}`;
  });

  ngOnInit(): void {
    this.jobStore.loadJobs();
  }
}
