import { Injectable, signal } from '@angular/core';
import { MockDataService } from '../services/mock-data.service';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class JobStore {
  readonly jobs = signal<Job[]>([]);
  readonly currentJob = signal<Job | undefined>(undefined);
  readonly similarJobs = signal<Job[]>([]);
  readonly featuredJobs = signal<Job[]>([]);
  readonly loading = signal(false);

  constructor(private dataService: MockDataService) {}

  loadJobs(): void {
    this.loading.set(true);
    this.dataService.getJobs().subscribe((jobs) => {
      this.jobs.set(jobs);
      this.loading.set(false);
    });
  }

  loadJob(id: string): void {
    this.loading.set(true);
    this.currentJob.set(undefined);
    this.similarJobs.set([]);
    this.dataService.getJobById(id).subscribe((job) => {
      this.currentJob.set(job);
      this.loading.set(false);
      if (job) {
        this.dataService.getSimilarJobs(job).subscribe((similar) => {
          this.similarJobs.set(similar);
        });
      }
    });
  }

  loadFeaturedJobs(): void {
    this.dataService.getFeaturedJobs().subscribe((jobs) => {
      this.featuredJobs.set(jobs);
    });
  }
}
