import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../services/api.service';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class SearchStore {
  private readonly api = inject(ApiService);

  readonly query = signal('');
  readonly jobType = signal('');
  readonly location = signal('');
  readonly results = signal<Job[]>([]);
  readonly loading = signal(false);
  readonly hasSearched = signal(false);

  search(query: string, type: string, location: string): void {
    this.query.set(query);
    this.jobType.set(type);
    this.location.set(location);
    this.loading.set(true);
    this.hasSearched.set(true);

    this.api.searchJobs({ query, jobType: type, location }).subscribe((jobs) => {
      this.results.set(jobs);
      this.loading.set(false);
    });
  }

  clear(): void {
    this.query.set('');
    this.jobType.set('');
    this.location.set('');
    this.hasSearched.set(false);
    this.results.set([]);
  }
}
