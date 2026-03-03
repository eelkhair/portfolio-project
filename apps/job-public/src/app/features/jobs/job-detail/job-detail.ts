import { Component, DestroyRef, inject, OnInit } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { JobTypeLabelPipe } from '../../../shared/pipes/job-type-label.pipe';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { SimilarJobs } from '../similar-jobs/similar-jobs';
import { JobStore } from '../../../core/stores/job.store';

@Component({
  selector: 'app-job-detail',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe, LoadingSpinner, SimilarJobs],
  templateUrl: './job-detail.html',
})
export class JobDetail implements OnInit {
  protected readonly store = inject(JobStore);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  ngOnInit(): void {
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
