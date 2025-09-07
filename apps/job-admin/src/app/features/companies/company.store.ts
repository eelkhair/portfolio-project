import {inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../core/services/company.service';
import {Company} from '../../core/types/models/Company';


@Injectable({ providedIn: 'root' })
export class CompanyStore{
  companyService = inject(CompanyService);
  companies = signal<Company[]>([])
  showCompanyDialog = signal(false)

  load = () => {
    this.companyService.list().subscribe(companies => {
      this.companies.set(companies);
    })
  }

  createCompany(company: Company) {

  }
}
