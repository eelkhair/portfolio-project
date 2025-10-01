import {Component, effect, inject, OnDestroy, signal} from '@angular/core';
import {CompanySelection} from '../../shared/companies/company-selection/company-selection';
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
  store = inject(JobsStore);
  constructor() {
    effect(() => {
      if(this.store.selectedCompany()) {
        this.store.loadJobs();
      }
    });
  }
}
