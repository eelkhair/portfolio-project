import { computed, inject, Injectable, signal } from '@angular/core';
import { ApplicationService } from '../../../core/services/application.service';
import {
  ApplicationDetail,
  ApplicationListItem,
  ApplicationStatus,
} from '../../../core/types/models/Application';
import { finalize, tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ReviewsStore {
  private readonly applicationService = inject(ApplicationService);

  readonly applications = signal<ApplicationListItem[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedApplication = signal<ApplicationDetail | null>(null);
  readonly detailLoading = signal(false);
  readonly detailVisible = signal(false);
  readonly selectedIds = signal<Set<string>>(new Set());

  readonly sortedByScore = computed(() =>
    [...this.applications()].sort((a, b) => (b.matchScore ?? 0) - (a.matchScore ?? 0))
  );

  readonly stats = computed(() => {
    const apps = this.applications();
    const pending = apps.filter(a => a.status === 'Submitted' || a.status === 'UnderReview').length;
    const shortlisted = apps.filter(a => a.status === 'Shortlisted').length;
    const scores = apps.filter(a => a.matchScore != null).map(a => a.matchScore!);
    const avgScore = scores.length > 0 ? Math.round(scores.reduce((s, v) => s + v, 0) / scores.length) : 0;
    return { pending, avgScore, shortlisted };
  });

  load() {
    this.loading.set(true);
    this.error.set(null);
    this.applicationService.list({ pageSize: 200, includeMatchScores: true }).pipe(
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
        next: res => {
          if (res.data) {
            // Carry over match data from list item
            const listItem = this.applications().find(a => a.id === id);
            if (listItem) {
              res.data.matchScore = listItem.matchScore;
              res.data.matchSummary = listItem.matchSummary;
              res.data.matchDetails = listItem.matchDetails;
              res.data.matchGaps = listItem.matchGaps;
            }
          }
          this.selectedApplication.set(res.data ?? null);
        },
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
          }
        },
      }),
    ).subscribe();
  }

  toggleSelect(id: string) {
    this.selectedIds.update(set => {
      const next = new Set(set);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  }

  selectAll() {
    const all = new Set(this.sortedByScore().map(a => a.id));
    this.selectedIds.set(all);
  }

  clearSelection() {
    this.selectedIds.set(new Set());
  }

  batchUpdateStatus(status: ApplicationStatus) {
    const ids = [...this.selectedIds()];
    if (ids.length === 0) return;

    this.applicationService.batchUpdateStatus(ids, status).pipe(
      tap({
        next: res => {
          if (res.data) {
            const updated = new Map(res.data.map(a => [a.id, a]));
            this.applications.update(list =>
              list.map(a => updated.has(a.id) ? { ...a, status: updated.get(a.id)!.status } : a)
            );
            this.clearSelection();
          }
        },
      }),
    ).subscribe();
  }

  initials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  statusSeverity(status: ApplicationStatus): 'info' | 'warn' | 'success' | 'danger' | 'contrast' {
    switch (status) {
      case 'Submitted': return 'info';
      case 'UnderReview': return 'warn';
      case 'Shortlisted': return 'success';
      case 'Rejected': return 'danger';
      case 'Accepted': return 'contrast';
    }
  }
}
