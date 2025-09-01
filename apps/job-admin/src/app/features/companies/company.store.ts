import {inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../core/services/company.service';
import {Company} from '../../core/types/companies/Company';


@Injectable({ providedIn: 'root' })
export class CompanyStore{

  companyService = inject(CompanyService);
  companies = signal<Company[]>([])

  load = () => {
    this.companyService.list().subscribe(companies => {
      this.companies.set(companies);

    })
  }
}
