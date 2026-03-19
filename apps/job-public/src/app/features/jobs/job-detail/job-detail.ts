import { Component, computed, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { JobTypeLabelPipe } from '../../../shared/pipes/job-type-label.pipe';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { SimilarJobs } from '../similar-jobs/similar-jobs';
import { JobStore } from '../../../core/stores/job.store';
import { ApplicationsListStore } from '../../../core/stores/applications-list.store';
import { AccountService } from '../../../core/services/account.service';
import { ProfileStore } from '../../../core/stores/profile.store';

@Component({
  selector: 'app-job-detail',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe, LoadingSpinner, SimilarJobs],
  templateUrl: './job-detail.html',
})
export class JobDetail implements OnInit {
  protected readonly store = inject(JobStore);
  protected readonly account = inject(AccountService);
  protected readonly profileStore = inject(ProfileStore);
  private readonly appStore = inject(ApplicationsListStore);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isApplied = computed(() => {
    const job = this.store.currentJob();
    return job ? this.appStore.appliedJobIds().has(job.id) : false;
  });

  protected readonly matchExplanation = computed(() => {
    const job = this.store.currentJob();
    if (!job) return null;
    return this.profileStore.matchingJobs().find(m => m.jobId === job.id) ?? null;
  });

  protected readonly hasMatchSidebar = computed(() => {
    // Default to 4-col layout while still loading to prevent layout flash
    if (this.account.isAuthenticated() && this.profileStore.matchingJobsLoading()) return true;
    return !!this.matchExplanation()?.matchSummary;
  });

  ngOnInit(): void {
    if (this.account.isAuthenticated()) {
      this.appStore.ensureLoaded();
      if (this.profileStore.matchingJobs().length === 0) {
        this.profileStore.loadResumes(true);
      }
    }

    this.route.paramMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(params => {
        const id = params.get('id');
        if (id) {
          this.store.loadJob(id);
        }
      });
  }
}
