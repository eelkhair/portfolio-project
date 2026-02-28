import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../services/api.service';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class JobStore {
  private readonly api = inject(ApiService);

  readonly jobs = signal<Job[]>([]);
  readonly currentJob = signal<Job | undefined>(undefined);
  readonly similarJobs = signal<Job[]>([]);
  readonly latestJobs = signal<Job[]>([]);
  readonly loading = signal(false);

  loadJobs(): void {
    this.loading.set(true);
    this.api.getJobs().subscribe((jobs) => {
      this.jobs.set(jobs);
      this.loading.set(false);
    });
  }

  loadJob(id: string): void {
    this.loading.set(true);
    this.currentJob.set(undefined);
    this.similarJobs.set([]);
    this.api.getJobById(id).subscribe((job) => {
      this.currentJob.set(job);
      this.loading.set(false);
      if (job) {
        this.api.getSimilarJobs(id, job.companyUId, job.jobType).subscribe((similar) => {
          this.similarJobs.set(similar);
        });
      }
    });
  }

  loadLatestJobs(): void {
    this.api.getLatestJobs(6).subscribe((jobs) => {
      this.latestJobs.set(jobs);
    });
  }
}
