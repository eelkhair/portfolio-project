import { Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SearchStore } from '../../../core/stores/search.store';

@Component({
  selector: 'app-job-search-bar',
  imports: [FormsModule],
  template: `
    <div class="card p-4">
      <div class="flex flex-col gap-3 md:flex-row">
        <div class="relative flex-1">
          <svg
            class="absolute left-3 top-1/2 h-5 w-5 -translate-y-1/2 text-slate-400"
            fill="none"
            viewBox="0 0 24 24"
            stroke="currentColor"
            stroke-width="2"
          >
            <path
              stroke-linecap="round"
              stroke-linejoin="round"
              d="M21 21l-5.197-5.197m0 0A7.5 7.5 0 105.196 5.196a7.5 7.5 0 0010.607 10.607z"
            />
          </svg>
          <input
            type="text"
            placeholder="Search jobs, skills, or companies..."
            [(ngModel)]="query"
            (keyup.enter)="onSearch()"
            class="input-field pl-10"
          />
        </div>

        <select [(ngModel)]="jobType" (change)="onSearch()" class="input-field md:w-44">
          <option value="">All Types</option>
          <option value="fullTime">Full-time</option>
          <option value="partTime">Part-time</option>
          <option value="contract">Contract</option>
          <option value="internship">Internship</option>
        </select>

        <input
          type="text"
          placeholder="Location..."
          [(ngModel)]="location"
          (keyup.enter)="onSearch()"
          class="input-field md:w-44"
        />

        <div class="flex gap-2">
          <button (click)="onSearch()" class="btn-primary whitespace-nowrap">Search</button>
          @if (searchStore.hasSearched()) {
            <button (click)="onClear()" class="btn-secondary whitespace-nowrap">Clear</button>
          }
        </div>
      </div>
    </div>
  `,
})
export class JobSearchBar {
  protected readonly searchStore = inject(SearchStore);
  readonly searched = output<void>();

  protected query = signal('');
  protected jobType = signal('');
  protected location = signal('');

  onSearch(): void {
    this.searchStore.search(this.query(), this.jobType(), this.location());
    this.searched.emit();
  }

  onClear(): void {
    this.query.set('');
    this.jobType.set('');
    this.location.set('');
    this.searchStore.clear();
  }
}
