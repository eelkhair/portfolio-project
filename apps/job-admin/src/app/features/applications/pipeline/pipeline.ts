import { Component, inject, OnInit } from '@angular/core';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';
import { Avatar } from 'primeng/avatar';
import { ProgressSpinner } from 'primeng/progressspinner';
import { PipelineStore } from './pipeline.store';
import { ApplicationDetailDialog } from './application-detail';
import { ApplicationStatus } from '../../../core/types/models/Application';

@Component({
  selector: 'app-pipeline',
  imports: [Card, Tag, Avatar, ProgressSpinner, ApplicationDetailDialog],
  templateUrl: './pipeline.html',
})
export class Pipeline implements OnInit {
  readonly store = inject(PipelineStore);

  draggedId: string | null = null;
  draggedFromStatus: ApplicationStatus | null = null;
  dragOverStage: ApplicationStatus | null = null;

  ngOnInit() {
    this.store.load();
  }

  onDragStart(event: DragEvent, id: string, fromStatus: ApplicationStatus) {
    this.draggedId = id;
    this.draggedFromStatus = fromStatus;
    event.dataTransfer?.setData('text/plain', id);
  }

  onDragOver(event: DragEvent, status: ApplicationStatus) {
    if (this.draggedFromStatus === status) return;
    event.preventDefault();
    this.dragOverStage = status;
  }

  onDrop(event: DragEvent, targetStatus: ApplicationStatus) {
    event.preventDefault();
    this.dragOverStage = null;
    if (this.draggedId && this.draggedFromStatus !== targetStatus) {
      this.store.updateStatus(this.draggedId, targetStatus);
    }
    this.draggedId = null;
    this.draggedFromStatus = null;
  }

  onDragEnd() {
    this.dragOverStage = null;
    this.draggedId = null;
    this.draggedFromStatus = null;
  }
}
