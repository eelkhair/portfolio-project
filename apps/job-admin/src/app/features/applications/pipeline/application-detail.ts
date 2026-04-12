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
  template: `
    <p-drawer
      [visible]="store.detailVisible()"
      (visibleChange)="store.detailVisible.set($event)"
      position="right"
      [style]="{width: '520px'}"
      (onHide)="store.closeDetail()">

      <ng-template #header>
        <span class="font-semibold text-lg">Application Detail</span>
      </ng-template>

      @if (store.detailLoading()) {
        <div class="flex justify-center py-10">
          <p-progressSpinner strokeWidth="3" />
        </div>
      } @else if (store.selectedApplication(); as app) {
        <div class="flex flex-col gap-4">
          <!-- Header -->
          <div>
            <h3 class="text-xl font-bold m-0">{{ app.applicantName }}</h3>
            <div class="text-sm text-color-secondary mt-1">{{ app.applicantEmail }}</div>
            <div class="text-sm text-color-secondary mt-1">
              Applied for <strong>{{ app.jobTitle }}</strong> at {{ app.companyName }}
            </div>
          </div>

          <!-- Status -->
          <div class="flex items-center gap-3">
            <span class="text-sm font-semibold">Status:</span>
            <p-select
              [options]="statusOptions"
              [(ngModel)]="selectedStatus"
              optionLabel="label"
              optionValue="value"
              [style]="{width: '180px'}"
              (onChange)="onStatusChange()" />
          </div>

          <p-divider />

          <!-- Personal Info -->
          @if (app.personalInfo) {
            <div>
              <h4 class="text-sm font-semibold text-color-secondary uppercase tracking-wide mb-2">Personal Info</h4>
              <div class="grid grid-cols-2 gap-2 text-sm">
                <div><span class="text-color-secondary">Name:</span> {{ app.personalInfo.firstName }} {{ app.personalInfo.lastName }}</div>
                <div><span class="text-color-secondary">Email:</span> {{ app.personalInfo.email }}</div>
                @if (app.personalInfo.phone) {
                  <div><span class="text-color-secondary">Phone:</span> {{ app.personalInfo.phone }}</div>
                }
                @if (app.personalInfo.linkedin) {
                  <div><span class="text-color-secondary">LinkedIn:</span> {{ app.personalInfo.linkedin }}</div>
                }
              </div>
            </div>
          }

          <!-- Skills -->
          @if (app.skills?.length) {
            <div>
              <h4 class="text-sm font-semibold text-color-secondary uppercase tracking-wide mb-2">Skills</h4>
              <div class="flex flex-wrap gap-1">
                @for (skill of app.skills; track skill) {
                  <p-tag [value]="skill" severity="info" />
                }
              </div>
            </div>
          }

          <!-- Work History -->
          @if (app.workHistory?.length) {
            <div>
              <h4 class="text-sm font-semibold text-color-secondary uppercase tracking-wide mb-2">Work History</h4>
              @for (w of app.workHistory; track w.company + w.jobTitle) {
                <div class="mb-3">
                  <div class="font-semibold text-sm">{{ w.jobTitle }}</div>
                  <div class="text-xs text-color-secondary">{{ w.company }} &middot; {{ w.startDate | slice:0:7 }} — {{ w.isCurrent ? 'Present' : (w.endDate | slice:0:7) }}</div>
                  @if (w.description) {
                    <div class="text-xs text-color-secondary mt-1">{{ w.description }}</div>
                  }
                </div>
              }
            </div>
          }

          <!-- Education -->
          @if (app.education?.length) {
            <div>
              <h4 class="text-sm font-semibold text-color-secondary uppercase tracking-wide mb-2">Education</h4>
              @for (e of app.education; track e.institution + e.degree) {
                <div class="mb-2">
                  <div class="font-semibold text-sm">{{ e.degree }}</div>
                  <div class="text-xs text-color-secondary">{{ e.institution }} {{ e.fieldOfStudy ? '· ' + e.fieldOfStudy : '' }}</div>
                </div>
              }
            </div>
          }

          <!-- Cover Letter -->
          @if (app.coverLetter) {
            <div>
              <h4 class="text-sm font-semibold text-color-secondary uppercase tracking-wide mb-2">Cover Letter</h4>
              <div class="text-sm whitespace-pre-wrap">{{ app.coverLetter }}</div>
            </div>
          }
        </div>
      }
    </p-drawer>
  `,
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
