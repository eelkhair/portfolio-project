import { Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeService } from '../../core/services/theme.service';
import { AccountService } from '../../core/services/account.service';
import { HelpPanelService } from '../../shared/help-panel/help-panel.service';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-header',
  imports: [RouterLink, RouterLinkActive],
  template: `
    <header class="bg-slate-900 text-white">
      <nav class="mx-auto flex max-w-7xl items-center justify-between px-6 py-4">
        <a routerLink="/" class="flex items-center gap-2">
          <img src="logo-icon.svg" alt="JobBoard" class="h-8 w-8" />
          <span class="text-xl font-bold">JobBoard</span>
          <span class="text-xs font-bold px-2 py-1 rounded"
                [class]="envName === 'DEV' ? 'bg-red-600 text-white' : envName === 'PROD' ? 'bg-green-600 text-white' : 'bg-amber-500 text-black'">
            {{ envName }}
          </span>
        </a>

        <div class="flex items-center gap-6">
          <a
            routerLink="/jobs"
            routerLinkActive="text-white"
            [routerLinkActiveOptions]="{ exact: false }"
            class="text-sm font-medium text-slate-300 transition-colors hover:text-white"
          >
            Jobs
          </a>
          <a
            routerLink="/companies"
            routerLinkActive="text-white"
            [routerLinkActiveOptions]="{ exact: false }"
            class="text-sm font-medium text-slate-300 transition-colors hover:text-white"
          >
            Companies
          </a>
          @if (account.isAuthenticated()) {
            <a
              routerLink="/profile"
              routerLinkActive="text-white"
              [routerLinkActiveOptions]="{ exact: false }"
              class="text-sm font-medium text-slate-300 transition-colors hover:text-white"
            >
              Profile
            </a>
          }
          @if (account.isAuthenticated()) {
            <a
              routerLink="/contact"
              routerLinkActive="text-white"
              [routerLinkActiveOptions]="{ exact: false }"
              class="text-sm font-medium text-slate-300 transition-colors hover:text-white"
            >
              Contact
            </a>
          }

          <button
            (click)="help.visible.set(true)"
            class="flex h-9 w-9 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-800 hover:text-white"
            aria-label="How to explore"
          >
            <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M9.879 7.519c1.171-1.025 3.071-1.025 4.242 0 1.172 1.025 1.172 2.687 0 3.712-.203.179-.43.326-.67.442-.745.361-1.45.999-1.45 1.827v.75M21 12a9 9 0 11-18 0 9 9 0 0118 0zm-9 5.25h.008v.008H12v-.008z" />
            </svg>
          </button>

          <button
            (click)="theme.toggle()"
            class="flex h-9 w-9 items-center justify-center rounded-lg text-slate-400 transition-colors hover:bg-slate-800 hover:text-white"
            [attr.aria-label]="theme.isDark() ? 'Switch to light mode' : 'Switch to dark mode'"
          >
            @if (theme.isDark()) {
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M12 3v2.25m6.364.386l-1.591 1.591M21 12h-2.25m-.386 6.364l-1.591-1.591M12 18.75V21m-4.773-4.227l-1.591 1.591M5.25 12H3m4.227-4.773L5.636 5.636M15.75 12a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0z"
                />
              </svg>
            } @else {
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M21.752 15.002A9.718 9.718 0 0118 15.75c-5.385 0-9.75-4.365-9.75-9.75 0-1.33.266-2.597.748-3.752A9.753 9.753 0 003 11.25C3 16.635 7.365 21 12.75 21a9.753 9.753 0 006.963-2.998z"
                />
              </svg>
            }
          </button>

          @if (account.isAuthenticated()) {
            <div class="relative">
              <button
                (click)="menuOpen.set(!menuOpen())"
                class="flex items-center gap-2 rounded-lg px-3 py-1.5 text-sm font-medium text-slate-300 transition-colors hover:bg-slate-800 hover:text-white"
              >
                <span>{{ account.displayName() }}</span>
                <svg class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                  <path stroke-linecap="round" stroke-linejoin="round" d="M19.5 8.25l-7.5 7.5-7.5-7.5" />
                </svg>
              </button>

              @if (menuOpen()) {
                <div class="absolute right-0 z-50 mt-2 w-48 rounded-lg bg-white py-1 shadow-lg ring-1 ring-black/5 dark:bg-slate-800 dark:ring-white/10">
                  <a
                    routerLink="/applications"
                    (click)="menuOpen.set(false)"
                    class="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-700"
                  >
                    My Applications
                  </a>
                  <button
                    (click)="account.logout(); menuOpen.set(false)"
                    class="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-slate-100 dark:text-slate-200 dark:hover:bg-slate-700"
                  >
                    Sign Out
                  </button>
                </div>
              }
            </div>
          } @else {
            <a
              routerLink="/signup"
              class="text-sm font-medium text-slate-300 transition-colors hover:text-white"
            >
              Sign Up
            </a>
            <button
              (click)="account.login()"
              class="rounded-lg bg-primary-600 px-4 py-1.5 text-sm font-medium text-white transition-colors hover:bg-primary-700"
            >
              Sign In
            </button>
          }
        </div>
      </nav>
    </header>
  `,
})
export class Header {
  protected readonly theme = inject(ThemeService);
  protected readonly account = inject(AccountService);
  protected readonly help = inject(HelpPanelService);
  protected readonly menuOpen = signal(false);
  protected readonly envName = environment.envName;
}
