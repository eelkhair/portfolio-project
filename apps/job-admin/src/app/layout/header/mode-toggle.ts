import {Component, inject} from '@angular/core';
import {SelectButton} from 'primeng/selectbutton';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {Tooltip} from 'primeng/tooltip';
import {FeatureFlagsService} from '../../core/services/feature-flags.service';

interface ModeOption {
  label: string;
  value: boolean;
  icon: string;
}

@Component({
  selector: 'app-mode-toggle',
  imports: [SelectButton, FormsModule, Button, Tooltip],
  templateUrl: './mode-toggle.html',
})
export class ModeToggle {
  featureFlags = inject(FeatureFlagsService);

  modeOptions: ModeOption[] = [
    {label: 'Monolith', value: true, icon: 'pi pi-box'},
    {label: 'Micro', value: false, icon: 'pi pi-th-large'},
  ];

  onToggle(value: boolean) {
    this.featureFlags.setLocalOverride(value);
  }

  reset() {
    this.featureFlags.clearLocalOverride();
  }
}
