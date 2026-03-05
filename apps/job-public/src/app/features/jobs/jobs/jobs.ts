import { Component, computed, inject, OnInit } from '@angular/core';
import { JobCard } from '../job-card/job-card';
import { JobSearchBar } from '../job-search-bar/job-search-bar';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { EmptyState } from '../../../shared/components/empty-state';
import { JobStore } from '../../../core/stores/job.store';
import { SearchStore } from '../../../core/stores/search.store';
import { ApplicationsListStore } from '../../../core/stores/applications-list.store';
import { AccountService } from '../../../core/services/account.service';

@Component({
  selector: 'app-jobs',
  imports: [JobCard, JobSearchBar, LoadingSpinner, EmptyState],
  templateUrl: './jobs.html',
})
export class Jobs implements OnInit {
  protected readonly jobStore = inject(JobStore);
  protected readonly searchStore = inject(SearchStore);
  private readonly appStore = inject(ApplicationsListStore);
  private readonly account = inject(AccountService);

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
    if (this.account.isAuthenticated()) {
      this.appStore.ensureLoaded();
    }
  }

  goToPage(page: number): void {
    if (this.searchStore.hasSearched()) {
      this.searchStore.goToPage(page);
    } else {
      this.jobStore.loadJobs(page);
    }
  }
}
