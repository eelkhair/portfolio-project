import { Component, effect, inject, OnInit } from '@angular/core';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { Avatar } from 'primeng/avatar';
import { ProgressBar } from 'primeng/progressbar';
import { ProgressSpinner } from 'primeng/progressspinner';
import { Checkbox } from 'primeng/checkbox';
import { Button } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';
import { Drawer } from 'primeng/drawer';
import { Divider } from 'primeng/divider';
import { Select } from 'primeng/select';
import { FormsModule } from '@angular/forms';
import { ReviewsStore } from './reviews.store';
import { ApplicationStatus } from '../../../core/types/models/Application';

interface StatusOption { label: string; value: ApplicationStatus; }

@Component({
  selector: 'app-reviews',
  imports: [Card, Tag, Avatar, ProgressBar, ProgressSpinner, Checkbox, Button, Tooltip, Drawer, Divider, Select, FormsModule],
  templateUrl: './reviews.html',
})
export class Reviews implements OnInit {
  readonly store = inject(ReviewsStore);

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

  statusOptions: StatusOption[] = this.allStatusOptions;
  selectedStatus: ApplicationStatus = 'Submitted';

  constructor() {
    effect(() => {
      const app = this.store.selectedApplication();
      if (app) {
        this.selectedStatus = app.status;
        const allowed = this.allowedTransitions[app.status] ?? [];
        this.statusOptions = this.allStatusOptions.filter(
          o => o.value === app.status || allowed.includes(o.value)
        );
      }
    });
  }

  ngOnInit() {
    this.store.load();
  }

  onStatusChange() {
    const app = this.store.selectedApplication();
    if (app && this.selectedStatus !== app.status) {
      this.store.updateStatus(app.id, this.selectedStatus);
    }
  }
}
