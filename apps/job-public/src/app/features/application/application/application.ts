import { Component, inject, OnDestroy, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ResumeUpload } from '../resume-upload/resume-upload';
import { ApplicationForm } from '../application-form/application-form';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { JobStore } from '../../../core/stores/job.store';
import { ApplicationStore } from '../../../core/stores/application.store';

@Component({
  selector: 'app-application',
  imports: [RouterLink, ResumeUpload, ApplicationForm, LoadingSpinner],
  templateUrl: './application.html',
})
export class Application implements OnInit, OnDestroy {
  protected readonly jobStore = inject(JobStore);
  protected readonly appStore = inject(ApplicationStore);
  private readonly route = inject(ActivatedRoute);
  protected readonly selectedResumeId = signal('');

  ngOnInit(): void {
    const jobId = this.route.snapshot.paramMap.get('jobId');
    if (jobId) {
      this.jobStore.loadJob(jobId);
    }
    this.appStore.loadProfile();
  }

  ngOnDestroy(): void {
    this.appStore.reset();
  }
}
