import { Component, inject, OnInit } from '@angular/core';
import { CompanyCard } from './components/company-card';
import { LoadingSpinner } from '../../shared/components/loading-spinner';
import { CompanyStore } from '../../core/stores/company.store';

@Component({
  selector: 'app-companies',
  imports: [CompanyCard, LoadingSpinner],
  template: `
    <div class="mx-auto max-w-7xl px-6 py-12">
      <div class="mb-8">
        <h1 class="section-heading text-3xl">Companies</h1>
        <p class="mt-2 section-subheading">Explore companies that are hiring.</p>
      </div>

      @if (store.loading()) {
        <app-loading-spinner label="Loading companies..." />
      } @else {
        <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
          @for (company of store.companies(); track company.id) {
            <app-company-card [company]="company" class="animate-fade-in" />
          }
        </div>
      }
    </div>
  `,
})
export class Companies implements OnInit {
  protected readonly store = inject(CompanyStore);

  ngOnInit(): void {
    this.store.loadCompanies();
  }
}
