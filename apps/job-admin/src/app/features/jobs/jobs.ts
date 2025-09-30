import {Component, effect, inject, OnDestroy} from '@angular/core';
import {CompanySelection} from '../../shared/companies/company-selection/company-selection';
import {CompanySelectionStore} from '../../shared/companies/company-selection/company-selection.store';
import {JobsStore} from './jobs.store';

@Component({
  selector: 'app-jobs',
  imports: [
    CompanySelection
  ],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css'
})
export class Jobs{
  companySelectionStore = inject(CompanySelectionStore);
  store = inject(JobsStore);
  selectedCompany = this.companySelectionStore.selectedCompany;
  constructor() {
    effect(() => {
      if(this.selectedCompany()) {
        console.log(this.selectedCompany());
      }
    });
  }
}
