import { Component, inject, OnInit } from '@angular/core';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { Avatar } from 'primeng/avatar';
import { ProgressSpinner } from 'primeng/progressspinner';
import { PipelineStore } from './pipeline.store';
import { ApplicationDetailDialog } from './application-detail';

@Component({
  selector: 'app-pipeline',
  imports: [Card, Tag, Avatar, ProgressSpinner, ApplicationDetailDialog],
  templateUrl: './pipeline.html',
})
export class Pipeline implements OnInit {
  readonly store = inject(PipelineStore);

  ngOnInit() {
    this.store.load();
  }
}
