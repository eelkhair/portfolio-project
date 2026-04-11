import { Component, HostListener, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HelpPanelService } from './help-panel.service';

@Component({
  selector: 'app-help-panel',
  imports: [RouterLink],
  template: `
    @if (open()) {
      <div
        class="fixed inset-0 z-50 flex items-start justify-center bg-black/40 backdrop-blur-sm animate-fade-in"
        (click)="onBackdropClick($event)"
      >
        <div
          class="mx-4 mt-20 w-full max-w-lg rounded-xl border border-slate-200 bg-white p-6 shadow-2xl dark:border-slate-700 dark:bg-slate-800"
          (click)="$event.stopPropagation()"
        >
          <div class="flex items-center justify-between mb-4">
            <h2 class="text-lg font-bold text-slate-900 dark:text-white">How to Explore</h2>
            <button
              (click)="close()"
              class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700 dark:hover:text-slate-200"
              aria-label="Close"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <p class="text-sm text-slate-500 dark:text-slate-400 mb-5">
            This is a distributed systems portfolio project. Try these features:
          </p>

          <div class="flex flex-col gap-2">
            <a routerLink="/jobs" (click)="close()"
               class="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/50 group">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-primary-100 text-primary-600 dark:bg-primary-900/30 dark:text-primary-400">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">Browse Jobs</div>
                <div class="text-xs text-slate-500 dark:text-slate-400">Search and filter job listings with real-time API calls.</div>
              </div>
              <svg class="h-4 w-4 mt-1 text-slate-300 group-hover:text-slate-500 dark:text-slate-600 dark:group-hover:text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
              </svg>
            </a>

            <a routerLink="/profile" (click)="close()"
               class="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/50 group">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-emerald-100 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-400">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 14.25v-2.625a3.375 3.375 0 00-3.375-3.375h-1.5A1.125 1.125 0 0113.5 7.125v-1.5a3.375 3.375 0 00-3.375-3.375H8.25m2.25 0H5.625c-.621 0-1.125.504-1.125 1.125v17.25c0 .621.504 1.125 1.125 1.125h12.75c.621 0 1.125-.504 1.125-1.125V11.25a9 9 0 00-9-9z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">Upload Resume</div>
                <div class="text-xs text-slate-500 dark:text-slate-400">Upload your resume and watch AI parse it in real-time via SignalR.</div>
              </div>
              <svg class="h-4 w-4 mt-1 text-slate-300 group-hover:text-slate-500 dark:text-slate-600 dark:group-hover:text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
              </svg>
            </a>

            <button (click)="close()"
               class="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/50 group text-left w-full">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-violet-100 text-violet-600 dark:bg-violet-900/30 dark:text-violet-400">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M8.625 12a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H8.25m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0H12m4.125 0a.375.375 0 11-.75 0 .375.375 0 01.75 0zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 01-2.555-.337A5.972 5.972 0 015.41 20.97a5.969 5.969 0 01-.474-.065 4.48 4.48 0 00.978-2.025c.09-.457-.133-.901-.467-1.226C3.93 16.178 3 14.189 3 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">AI Chat</div>
                <div class="text-xs text-slate-500 dark:text-slate-400">Ask the AI to find matching jobs, analyze your resume, or get recommendations.</div>
              </div>
            </button>

            <a routerLink="/jobs" (click)="close()"
               class="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/50 group">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-amber-100 text-amber-600 dark:bg-amber-900/30 dark:text-amber-400">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M9 12h3.75M9 15h3.75M9 18h3.75m3 .75H18a2.25 2.25 0 002.25-2.25V6.108c0-1.135-.845-2.098-1.976-2.192a48.424 48.424 0 00-1.123-.08m-5.801 0c-.065.21-.1.433-.1.664 0 .414.336.75.75.75h4.5a.75.75 0 00.75-.75 2.25 2.25 0 00-.1-.664m-5.8 0A2.251 2.251 0 0113.5 2.25H15c1.012 0 1.867.668 2.15 1.586m-5.8 0c-.376.023-.75.05-1.124.08C9.095 4.01 8.25 4.973 8.25 6.108V8.25m0 0H4.875c-.621 0-1.125.504-1.125 1.125v11.25c0 .621.504 1.125 1.125 1.125h9.75c.621 0 1.125-.504 1.125-1.125V9.375c0-.621-.504-1.125-1.125-1.125H8.25zM6.75 12h.008v.008H6.75V12zm0 3h.008v.008H6.75V15zm0 3h.008v.008H6.75V18z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">Apply to Jobs</div>
                <div class="text-xs text-slate-500 dark:text-slate-400">Pick a job and submit a multi-section application form.</div>
              </div>
              <svg class="h-4 w-4 mt-1 text-slate-300 group-hover:text-slate-500 dark:text-slate-600 dark:group-hover:text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M8.25 4.5l7.5 7.5-7.5 7.5" />
              </svg>
            </a>

            <button (click)="close()"
               class="flex items-start gap-3 rounded-lg p-3 transition-colors hover:bg-slate-50 dark:hover:bg-slate-700/50 group text-left w-full">
              <div class="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-slate-100 text-slate-600 dark:bg-slate-700 dark:text-slate-400">
                <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M11.42 15.17l-5.1-5.1a1.5 1.5 0 010-2.12l.88-.88a1.5 1.5 0 012.12 0l2.83 2.83 5.66-5.66a1.5 1.5 0 012.12 0l.88.88a1.5 1.5 0 010 2.12l-7.78 7.78a1.5 1.5 0 01-2.12 0z" />
                </svg>
              </div>
              <div class="flex-1">
                <div class="text-sm font-semibold text-slate-900 dark:text-white">Debug Mode</div>
                <div class="text-xs text-slate-500 dark:text-slate-400">Open the debug bar (bottom-left) to see API traces, Jaeger links, and Grafana dashboards.</div>
              </div>
            </button>
          </div>
        </div>
      </div>
    }
  `,
})
export class HelpPanel {
  private helpService = inject(HelpPanelService);
  readonly open = this.helpService.visible;

  close() {
    this.open.set(false);
  }

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.open()) {
      this.close();
    }
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.close();
    }
  }
}
