import { computed, inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../services/api.service';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly api = inject(ApiService);
  private readonly pageSize = 8;

  readonly query = signal('');
  readonly jobType = signal('');
  readonly location = signal('');
  private readonly allResults = signal<Job[]>([]);
  readonly loading = signal(false);
  readonly hasSearched = signal(false);
  readonly currentPage = signal(1);

  readonly totalCount = computed(() => this.allResults().length);
  readonly totalPages = computed(() => Math.ceil(this.allResults().length / this.pageSize));
  readonly hasPreviousPage = computed(() => this.currentPage() > 1);
  readonly hasNextPage = computed(() => this.currentPage() < this.totalPages());

  readonly results = computed(() => {
    const start = (this.currentPage() - 1) * this.pageSize;
    return this.allResults().slice(start, start + this.pageSize);
  });

  search(query: string, type: string, location: string): void {
    this.query.set(query);
    this.jobType.set(type);
    this.location.set(location);
    this.loading.set(true);
    this.hasSearched.set(true);
    this.currentPage.set(1);

    this.api.searchJobs({ query, jobType: type, location }).subscribe((jobs) => {
      this.allResults.set(jobs);
      this.loading.set(false);
    });
  }

  goToPage(page: number): void {
    this.currentPage.set(page);
  }

  clear(): void {
    this.query.set('');
    this.jobType.set('');
    this.location.set('');
    this.hasSearched.set(false);
    this.allResults.set([]);
    this.currentPage.set(1);
  }
}
