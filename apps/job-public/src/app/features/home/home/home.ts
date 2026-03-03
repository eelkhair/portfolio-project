import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobCard } from '../../jobs/job-card/job-card';
import { JobStore } from '../../../core/stores/job.store';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, JobCard],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  protected readonly store = inject(JobStore);
  private readonly api = inject(ApiService);

  readonly jobCount = signal(0);
  readonly companyCount = signal(0);

  ngOnInit(): void {
    this.store.loadLatestJobs();
    this.api.getStats().subscribe((stats) => {
      this.jobCount.set(stats.jobCount);
      this.companyCount.set(stats.companyCount);
    });
  }
}
