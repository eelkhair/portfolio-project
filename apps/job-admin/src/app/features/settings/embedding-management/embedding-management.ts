import {Component, inject, signal} from '@angular/core';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {SettingsService} from '../../../core/services/settings.service';
import {NotificationService} from '../../../core/services/notification.service';

@Component({
  selector: 'app-embedding-management',
  imports: [
    Button,
    Card,
  ],
  templateUrl: './embedding-management.html',
})
export class EmbeddingManagement {
  private settingsService = inject(SettingsService);
  private notificationService = inject(NotificationService);

  loading = signal(false);

  reEmbedJobs() {
    this.loading.set(true);
    this.settingsService.reEmbedAllJobs().subscribe({
      next: (response) => {
        const count = response.data?.jobsProcessed ?? 0;
        this.notificationService.success(
          'Re-embed Complete',
          `Successfully re-embedded ${count} job${count !== 1 ? 's' : ''}.`
        );
        this.loading.set(false);
      },
      error: (err) => {
        this.notificationService.error(
          'Error',
          err.error?.exceptions?.message ?? 'Failed to re-embed jobs.'
        );
        this.loading.set(false);
      }
    });
  }
}
