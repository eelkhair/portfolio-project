import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-footer',
  imports: [RouterLink],
  template: `
    <footer class="bg-slate-900 text-white">
      <div class="mx-auto max-w-7xl px-6 py-12">
        <div class="grid grid-cols-1 gap-8 md:grid-cols-3">
          <div>
            <div class="flex items-center gap-2">
              <div
                class="flex h-8 w-8 items-center justify-center rounded-lg bg-primary-600 text-sm font-bold"
              >
                JB
              </div>
              <span class="text-lg font-bold">JobBoard</span>
            </div>
            <p class="mt-3 text-sm text-slate-400">
              A distributed systems portfolio project demonstrating modern backend architecture.
            </p>
          </div>

          <div>
            <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-400">
              Navigation
            </h3>
            <ul class="mt-3 space-y-2">
              <li>
                <a routerLink="/jobs" class="text-sm text-slate-300 hover:text-white">
                  Browse Jobs
                </a>
              </li>
              <li>
                <a routerLink="/companies" class="text-sm text-slate-300 hover:text-white">
                  Companies
                </a>
              </li>
            </ul>
          </div>

          <div>
            <h3 class="text-sm font-semibold uppercase tracking-wider text-slate-400">
              Architecture
            </h3>
            <ul class="mt-3 space-y-2">
              <li class="text-sm text-slate-400">Monolith + CQRS</li>
              <li class="text-sm text-slate-400">Microservices</li>
              <li class="text-sm text-slate-400">Strangler-Fig Pattern</li>
            </ul>
          </div>
        </div>

        <div class="mt-10 border-t border-slate-800 pt-6">
          <p class="text-center text-sm text-slate-500">
            &copy; {{ currentYear }} JobBoard. Portfolio project by Elkhair.
          </p>
        </div>
      </div>
    </footer>
  `,
})
export class Footer {
  readonly currentYear = new Date().getFullYear();
}
