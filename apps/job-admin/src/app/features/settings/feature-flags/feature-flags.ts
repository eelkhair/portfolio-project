import {Component, inject, OnInit, signal, ViewEncapsulation} from '@angular/core';
import {Card} from 'primeng/card';
import {ToggleSwitch, ToggleSwitchChangeEvent} from 'primeng/toggleswitch';
import {FormsModule} from '@angular/forms';
import {SettingsService} from '../../../core/services/settings.service';
import {NotificationService} from '../../../core/services/notification.service';
import {ProgressSpinner} from 'primeng/progressspinner';

interface FeatureFlag {
  name: string;
  enabled: boolean;
  saving: boolean;
}

@Component({
  selector: 'app-feature-flags',
  imports: [
    FormsModule,
    Card,
    ToggleSwitch,
    ProgressSpinner,
  ],
  templateUrl: './feature-flags.html',
  styleUrl: './feature-flags.css',
  encapsulation: ViewEncapsulation.None
})
export class FeatureFlags implements OnInit {
  private settingsService = inject(SettingsService);
  private notificationService = inject(NotificationService);

  flags = signal<FeatureFlag[]>([]);
  initialLoading = signal(true);

  ngOnInit() {
    this.loadFlags();
  }

  private loadFlags() {
    this.settingsService.getFeatureFlags().subscribe({
      next: (response) => {
        if (response.data) {
          this.flags.set(response.data.map(f => ({...f, saving: false})));
        }
        this.initialLoading.set(false);
      },
      error: () => {
        this.initialLoading.set(false);
      }
    });
  }

  onToggle(name: string, event: ToggleSwitchChangeEvent) {
    const enabled = event.checked;

    this.flags.update(flags =>
      flags.map(f => f.name === name ? {...f, enabled, saving: true} : f)
    );

    this.settingsService.updateFeatureFlag({name, enabled}).subscribe({
      next: () => {
        this.notificationService.success('Flag Updated', `${name} set to ${enabled ? 'enabled' : 'disabled'}.`);
        this.flags.update(flags =>
          flags.map(f => f.name === name ? {...f, saving: false} : f)
        );
      },
      error: (err) => {
        this.notificationService.error('Error', err.error?.exceptions?.message ?? 'Failed to update feature flag.');
        this.flags.update(flags =>
          flags.map(f => f.name === name ? {...f, enabled: !enabled, saving: false} : f)
        );
      }
    });
  }
}
