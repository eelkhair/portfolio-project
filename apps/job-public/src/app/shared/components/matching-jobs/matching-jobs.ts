import { Component, ElementRef, input, signal, viewChild } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatchingJob } from '../../../core/types/job.type';

@Component({
  selector: 'app-matching-jobs',
  imports: [RouterLink],
  templateUrl: './matching-jobs.html',
})
export class MatchingJobs {
  jobs = input.required<MatchingJob[]>();
  loading = input(false);
  error = input<string | null>(null);
  hasResumes = input(false);

  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('carousel');
  readonly canScrollLeft = signal(false);
  readonly canScrollRight = signal(true);

  matchColor(similarity: number): string {
    if (similarity >= 0.8) return 'bg-green-100 text-green-700 dark:bg-green-900/20 dark:text-green-400';
    if (similarity >= 0.6) return 'bg-amber-100 text-amber-700 dark:bg-amber-900/20 dark:text-amber-400';
    return 'bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-400';
  }

  matchPercent(similarity: number): string {
    return Math.round(similarity * 100) + '%';
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
