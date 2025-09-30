import {Component, inject, OnInit} from '@angular/core';
import {JobsStore} from '../../../features/jobs/jobs.store';
import {CompanySelectionStore} from './company-selection.store';
import {Select} from 'primeng/select';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-company-selection',
  imports: [
    Select,
    Button
  ],
  templateUrl: './company-selection.html',
  styleUrl: './company-selection.css'
})
export class CompanySelection implements OnInit {
  store = inject(CompanySelectionStore);
  ngOnInit() {
    if (!this.store.selectedCompany()) {
      this.store.populateCompanies();
    }
  }
  selectCompany(value: string) {
    const company = this.store.companiesList().find(c => c.uId === value);
    if (company) {
      this.store.selectedCompany.set(company);
    }
  }
}
