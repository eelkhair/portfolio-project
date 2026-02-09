  import {Component, inject, OnInit, signal} from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {AutoComplete, AutoCompleteCompleteEvent} from 'primeng/autocomplete';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {SettingsService} from '../../../core/services/settings.service';
import {NotificationService} from '../../../core/services/notification.service';
import {ProgressSpinner} from 'primeng/progressspinner';

interface ProviderOption {
  value: string;
  label: string;
  model: string;
}

@Component({
  selector: 'app-ai-provider',
  imports: [
    ReactiveFormsModule,
    AutoComplete,
    Button,
    Card,
    ProgressSpinner,
  ],
  templateUrl: './ai-provider.html',
  styleUrl: './ai-provider.css'
})
export class AiProvider implements OnInit {
  private settingsService = inject(SettingsService);
  private notificationService = inject(NotificationService);

  providers: ProviderOption[] = [
    {value: 'azure', label: 'Azure', model: 'gpt-4o-mini'},
    {value: 'openai', label: 'OpenAI', model: 'gpt-4.1-mini'},
    {value: 'gemini', label: 'Gemini', model: 'gemini-2.0-flash-lite'},
    {value: 'claude', label: 'Claude', model: 'claude-haiku-4-5'},
  ];

  providerSuggestions = signal<string[]>([]);
  modelSuggestions = signal<string[]>([]);
  loading = signal(false);
  initialLoading = signal(true);

  form = new FormGroup({
    provider: new FormControl<string>('', Validators.required),
    model: new FormControl<string>('', Validators.required),
  });

  ngOnInit() {
    this.loadCurrentSettings();
  }

  private loadCurrentSettings() {
    this.settingsService.getProvider().subscribe({
      next: (response) => {
        if (response.data) {
          const provider = this.providers.find(p => p.value === response.data!.provider);
          this.form.controls.provider.setValue(provider?.label ?? response.data.provider);
          this.form.controls.model.setValue(response.data.model);
        }
        this.initialLoading.set(false);
      },
      error: () => {
        this.initialLoading.set(false);
      }
    });
  }

  onCompleteProvider(event: AutoCompleteCompleteEvent) {
    const query = event.query.toLowerCase();
    this.providerSuggestions.set(
      this.providers
        .filter(p => p.label.toLowerCase().includes(query) || p.value.includes(query))
        .map(p => p.label)
    );
  }

  onCompleteModel(event: AutoCompleteCompleteEvent) {
    const query = event.query.toLowerCase();
    const providerValue = this.form.controls.provider.value?.toLowerCase();
    const provider = this.providers.find(p => p.value === providerValue || p.label.toLowerCase() === providerValue);

    if (provider) {
      // Show only the model for the selected provider
      const filtered = provider.model.toLowerCase().includes(query) ? [provider.model] : [];
      this.modelSuggestions.set(filtered);
    } else {
      // No provider selected, show all models
      const models = this.providers.map(p => p.model);
      this.modelSuggestions.set(models.filter(m => m.toLowerCase().includes(query)));
    }
  }

  onProviderChange() {
    const providerValue = this.form.controls.provider.value?.toLowerCase();
    const provider = this.providers.find(p => p.value === providerValue || p.label.toLowerCase() === providerValue);
    if (provider) {
      this.form.controls.model.setValue(provider.model);
    }
  }

  saveSettings() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    const providerValue = this.form.controls.provider.value!.toLowerCase();
    const provider = this.providers.find(p => p.value === providerValue || p.label.toLowerCase() === providerValue);

    this.settingsService.updateProvider({
      provider: provider?.value ?? providerValue,
      model: this.form.controls.model.value!
    }).subscribe({
      next: () => {
        this.notificationService.success('Settings Saved', 'AI provider settings updated successfully.');
        this.loading.set(false);
      },
      error: (err) => {
        this.notificationService.error('Error', err.error?.exceptions?.message ?? 'Failed to update settings.');
        this.loading.set(false);
      }
    });
  }
}
