import { Injectable, signal } from '@angular/core';
import { MockDataService } from '../services/mock-data.service';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class SearchStore {
  readonly query = signal('');
  readonly jobType = signal('');
  readonly location = signal('');
  readonly results = signal<Job[]>([]);
  readonly loading = signal(false);
  readonly hasSearched = signal(false);

  constructor(private dataService: MockDataService) {}

  search(query: string, type: string, location: string): void {
    this.query.set(query);
    this.jobType.set(type);
    this.location.set(location);
    this.loading.set(true);
    this.hasSearched.set(true);

    this.dataService.searchJobs(query, type, location).subscribe((jobs) => {
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
