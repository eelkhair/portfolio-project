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

@Component({
  selector: 'app-job-detail',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe, LoadingSpinner, SimilarJobs],
  templateUrl: './job-detail.html',
})
export class JobDetail implements OnInit {
  protected readonly store = inject(JobStore);
  protected readonly account = inject(AccountService);
  private readonly appStore = inject(ApplicationsListStore);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly isApplied = computed(() => {
    const job = this.store.currentJob();
    return job ? this.appStore.appliedJobIds().has(job.id) : false;
  });

  ngOnInit(): void {
    if (this.account.isAuthenticated()) {
      this.appStore.ensureLoaded();
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
