import { Component, ElementRef, inject, input, output, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatchingJob } from '../../../core/types/job.type';
import { ApplicationsListStore } from '../../../core/stores/applications-list.store';

@Component({
  selector: 'app-matching-jobs',
  imports: [RouterLink],
  templateUrl: './matching-jobs.html',
})
export class MatchingJobs {
  private readonly appStore = inject(ApplicationsListStore);
  jobs = input.required<MatchingJob[]>();
  loading = input(false);
  error = input<string | null>(null);
  hasResumes = input(false);
  readonly reEmbedClick = output<void>();

  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('carousel');
  readonly canScrollLeft = signal(false);
  readonly canScrollRight = signal(true);

  isApplied(jobId: string): boolean {
    return this.appStore.appliedJobIds().has(jobId);
  }

  matchPercent(similarity: number): string {
    return Math.round(similarity * 100) + '%';
  }

  matchLevel(similarity: number): string {
    const pct = Math.round(similarity * 100);
    if (pct >= 80) return 'high';
    if (pct >= 60) return 'medium';
    return 'low';
  }

  scroll(direction: 'left' | 'right'): void {
    const el = this.scrollContainer()?.nativeElement;
    if (!el) return;
    const cardWidth = 304; // 280px card + 24px gap
    el.scrollBy({ left: direction === 'left' ? -cardWidth : cardWidth, behavior: 'smooth' });
  }

  onScroll(): void {
    const el = this.scrollContainer()?.nativeElement;
    if (!el) return;
    this.canScrollLeft.set(el.scrollLeft > 0);
    this.canScrollRight.set(el.scrollLeft + el.clientWidth < el.scrollWidth - 1);
  }
}
