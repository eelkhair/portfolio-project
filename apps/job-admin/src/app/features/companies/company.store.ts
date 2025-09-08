import {inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../core/services/company.service';
import {Company} from '../../core/types/models/Company';
import {Industry} from '../../core/types/models/Industry';
import {CreateCompanyDto} from '../../core/types/Dtos/CreateCompanyDto';


@Injectable({ providedIn: 'root' })
export class CompanyStore{
  companyService = inject(CompanyService);
  companies = signal<Company[]>([])
  industries = signal<Industry[]>([])
  showCompanyDialog = signal(false)


  load = () => {
    this.companyService.list().subscribe(companies => {
      this.companies.set(companies);
    })
    this.companyService.listIndustries().subscribe(industries => {
      this.industries.set(industries);
    })
  }

  createCompany(company: CreateCompanyDto) {
    this.companyService.createCompany(company).subscribe(company => {

    })
  }

}
