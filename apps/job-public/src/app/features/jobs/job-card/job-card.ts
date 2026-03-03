import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { DateAgoPipe } from '../../../shared/pipes/date-ago.pipe';
import { JobTypeLabelPipe } from '../../../shared/pipes/job-type-label.pipe';
import { Job } from '../../../core/types/job.type';

@Component({
  selector: 'app-job-card',
  imports: [RouterLink, DateAgoPipe, JobTypeLabelPipe],
  templateUrl: './job-card.html',
})
export class JobCard {
  job = input.required<Job>();
}
