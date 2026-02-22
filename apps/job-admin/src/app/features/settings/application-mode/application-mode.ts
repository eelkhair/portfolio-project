import {Component, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {SelectButton} from 'primeng/selectbutton';
import {FormsModule} from '@angular/forms';
import {SettingsService} from '../../../core/services/settings.service';
import {NotificationService} from '../../../core/services/notification.service';
import {ProgressSpinner} from 'primeng/progressspinner';

interface ModeOption {
  label: string;
  value: boolean;
  icon: string;
  description: string;
}

@Component({
  selector: 'app-application-mode',
  imports: [
    FormsModule,
    Button,
    Card,
    SelectButton,
    ProgressSpinner,
  ],
  templateUrl: './application-mode.html',
  styleUrl: './application-mode.css',
  encapsulation: ViewEncapsulation.None
})
export class ApplicationMode implements OnInit {
  private settingsService = inject(SettingsService);
  private notificationService = inject(NotificationService);

  modeOptions: ModeOption[] = [
    {label: 'Monolith', value: true, icon: 'pi pi-box', description: 'Single deployable unit — all bounded contexts in one service.'},
    {label: 'Microservices', value: false, icon: 'pi pi-th-large', description: 'Distributed services — each bounded context runs independently.'},
  ];

  selectedMode = signal<boolean>(true);
  loading = signal(false);
  initialLoading = signal(true);

  get currentModeOption(): ModeOption {
    return this.modeOptions.find(o => o.value === this.selectedMode())!;
  }

  ngOnInit() {
    this.loadCurrentMode();
  }

  private loadCurrentMode() {
    this.settingsService.getApplicationMode().subscribe({
      next: (response) => {
        if (response.data) {
          this.selectedMode.set(response.data.isMonolith);
        }
        this.initialLoading.set(false);
      },
      error: () => {
        this.initialLoading.set(false);
      }
    });
  }

  saveSettings() {
    this.loading.set(true);
    this.settingsService.updateApplicationMode({isMonolith: this.selectedMode()}).subscribe({
      next: () => {
        const label = this.currentModeOption.label;
        this.notificationService.success('Mode Updated', `Application mode set to ${label}.`);
        this.loading.set(false);
      },
      error: (err) => {
        this.notificationService.error('Error', err.error?.exceptions?.message ?? 'Failed to update application mode.');
        this.loading.set(false);
      }
    });
  }
}
