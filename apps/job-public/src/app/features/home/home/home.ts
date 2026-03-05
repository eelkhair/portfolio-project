import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JobCard } from '../../jobs/job-card/job-card';
import { MatchingJobs } from '../../../shared/components/matching-jobs/matching-jobs';
import { JobStore } from '../../../core/stores/job.store';
import { ProfileStore } from '../../../core/stores/profile.store';
import { AccountService } from '../../../core/services/account.service';
import { ApiService } from '../../../core/services/api.service';

@Component({
  selector: 'app-home',
  imports: [RouterLink, JobCard, MatchingJobs],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  protected readonly store = inject(JobStore);
  protected readonly profileStore = inject(ProfileStore);
  protected readonly account = inject(AccountService);
  private readonly api = inject(ApiService);

  readonly jobCount = signal(0);
  readonly companyCount = signal(0);

  ngOnInit(): void {
    this.store.loadLatestJobs();
    this.api.getStats().subscribe((stats) => {
      this.jobCount.set(stats.jobCount);
      this.companyCount.set(stats.companyCount);
    });

    if (this.account.isAuthenticated()) {
      this.profileStore.loadResumes(true);
    }
  }
}
