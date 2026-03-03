import { Component, inject, OnInit } from '@angular/core';
import { CompanyCard } from '../company-card/company-card';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { CompanyStore } from '../../../core/stores/company.store';

@Component({
  selector: 'app-companies',
  imports: [CompanyCard, LoadingSpinner],
  templateUrl: './companies.html',
})
export class Companies implements OnInit {
  protected readonly store = inject(CompanyStore);

  ngOnInit(): void {
    this.store.loadCompanies();
  }
}
