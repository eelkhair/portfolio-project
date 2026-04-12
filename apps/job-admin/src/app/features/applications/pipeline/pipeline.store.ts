import { computed, inject, Injectable, signal } from '@angular/core';
import { ApplicationService, ApplicationFilters } from '../../../core/services/application.service';
import {
  ApplicationDetail,
  ApplicationListItem,
  ApplicationStatus,
  PipelineStage,
} from '../../../core/types/models/Application';
import { finalize, tap } from 'rxjs';

const STAGE_CONFIG: { name: string; status: ApplicationStatus; severity: PipelineStage['severity'] }[] = [
  { name: 'Applied', status: 'Submitted', severity: 'info' },
  { name: 'Screening', status: 'UnderReview', severity: 'warn' },
  { name: 'Interview', status: 'Shortlisted', severity: 'success' },
  { name: 'Offered', status: 'Accepted', severity: 'contrast' },
];

@Injectable({ providedIn: 'root' })
export class PipelineStore {
  private readonly applicationService = inject(ApplicationService);

  readonly applications = signal<ApplicationListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedApplication = signal<ApplicationDetail | null>(null);
  readonly detailLoading = signal(false);
  readonly detailVisible = signal(false);

  readonly stages = computed<PipelineStage[]>(() => {
    const apps = this.applications();
    return STAGE_CONFIG.map(config => ({
      ...config,
      candidates: apps.filter(a => a.status === config.status),
    }));
  });

  readonly rejectedCount = computed(() =>
    this.applications().filter(a => a.status === 'Rejected').length
  );

  readonly totalCount = computed(() => this.applications().length);

  load(filters?: ApplicationFilters) {
    this.loading.set(true);
    this.error.set(null);
    this.applicationService.list({ ...filters, pageSize: 200 }).pipe(
      tap({
        next: res => this.applications.set(res.data?.items ?? []),
        error: err => this.error.set(err.message ?? 'Failed to load applications'),
      }),
      finalize(() => this.loading.set(false)),
    ).subscribe();
  }

  selectApplication(id: string) {
    this.detailLoading.set(true);
    this.detailVisible.set(true);
    this.applicationService.getDetail(id).pipe(
      tap({
        next: res => this.selectedApplication.set(res.data ?? null),
        error: () => this.selectedApplication.set(null),
      }),
      finalize(() => this.detailLoading.set(false)),
    ).subscribe();
  }

  closeDetail() {
    this.detailVisible.set(false);
    this.selectedApplication.set(null);
  }

  updateStatus(id: string, status: ApplicationStatus) {
    this.applicationService.updateStatus(id, status).pipe(
      tap({
        next: res => {
          if (res.data) {
            this.applications.update(list =>
              list.map(a => a.id === id ? { ...a, status: res.data!.status } : a)
            );
            if (this.selectedApplication()?.id === id) {
              this.selectedApplication.update(d => d ? { ...d, status: res.data!.status } : d);
            }
          }
        },
      }),
    ).subscribe();
  }

  daysAgo(dateStr: string): number {
    const diff = Date.now() - new Date(dateStr).getTime();
    return Math.floor(diff / (1000 * 60 * 60 * 24));
  }

  initials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}
