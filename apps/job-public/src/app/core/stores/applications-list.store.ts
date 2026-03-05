import { computed, inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../services/api.service';
import { ApplicationResponse } from '../types/application.type';

@Injectable({ providedIn: 'root' })
export class ApplicationsListStore {
  private readonly api = inject(ApiService);
  private loaded = false;

  readonly applications = signal<ApplicationResponse[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly appliedJobIds = computed(() =>
    new Set(this.applications().map(a => a.jobId)),
  );

  /** Load once per session; call from any page that needs applied status. */
  ensureLoaded(): void {
    if (this.loaded) return;
    this.loadApplications();
  }

  loadApplications(): void {
    this.loaded = true;
    this.loading.set(true);
    this.error.set(null);

    this.api.getApplications().subscribe({
      next: (data) => {
        this.applications.set(data);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load applications.');
        this.loading.set(false);
      },
    });
  }
}
