import { Component, computed, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { JobTypeLabelPipe } from '../../../shared/pipes/job-type-label.pipe';
import { Job } from '../../../core/types/job.type';
import { ApplicationsListStore } from '../../../core/stores/applications-list.store';

@Component({
  selector: 'app-job-card',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe],
  templateUrl: './job-card.html',
})
export class JobCard {
  private readonly appStore = inject(ApplicationsListStore);
  job = input.required<Job>();
  isApplied = computed(() => this.appStore.appliedJobIds().has(this.job().id));
}
