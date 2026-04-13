import { Component, inject } from '@angular/core';
import { SlicePipe } from '@angular/common';
import { Drawer } from 'primeng/drawer';
import { Tag } from 'primeng/tag';
import { Select } from 'primeng/select';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Divider } from 'primeng/divider';
import { FormsModule } from '@angular/forms';
import { PipelineStore } from './pipeline.store';
import { ApplicationStatus } from '../../../core/types/models/Application';

interface StatusOption {
  label: string;
  value: ApplicationStatus;
}

@Component({
  selector: 'app-application-detail',
  imports: [Drawer, Tag, Select, ProgressSpinner, Divider, FormsModule, SlicePipe],
  templateUrl: './application-detail.html',
})
export class ApplicationDetailDialog {
  readonly store = inject(PipelineStore);

  private readonly allStatusOptions: StatusOption[] = [
    { label: 'Submitted', value: 'Submitted' },
    { label: 'Under Review', value: 'UnderReview' },
    { label: 'Shortlisted', value: 'Shortlisted' },
    { label: 'Rejected', value: 'Rejected' },
    { label: 'Accepted', value: 'Accepted' },
  ];

  private readonly allowedTransitions: Record<ApplicationStatus, ApplicationStatus[]> = {
    Submitted: ['UnderReview', 'Shortlisted', 'Rejected'],
    UnderReview: ['Shortlisted', 'Rejected'],
    Shortlisted: ['Accepted', 'Rejected'],
    Rejected: ['UnderReview'],
    Accepted: [],
  };

  statusOptions: StatusOption[] = [];
  selectedStatus: ApplicationStatus = 'Submitted';

  constructor() {
    const checkInterval = setInterval(() => {
      const app = this.store.selectedApplication();
      if (app) {
        this.selectedStatus = app.status;
        const allowed = this.allowedTransitions[app.status] ?? [];
        this.statusOptions = this.allStatusOptions.filter(
          o => o.value === app.status || allowed.includes(o.value)
        );
        clearInterval(checkInterval);
      }
    }, 100);
    setTimeout(() => clearInterval(checkInterval), 5000);
  }

  onStatusChange() {
    const app = this.store.selectedApplication();
    if (app && this.selectedStatus !== app.status) {
      this.store.updateStatus(app.id, this.selectedStatus);
    }
  }
}
