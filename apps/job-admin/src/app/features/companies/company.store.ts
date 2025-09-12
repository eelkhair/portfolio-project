import {inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../core/services/company.service';
import {Company} from '../../core/types/models/Company';
import {Industry} from '../../core/types/models/Industry';
import {CreateCompanyDto} from '../../core/types/Dtos/CreateCompanyDto';
import {NotificationService} from '../../core/services/notification.service';
import {tap} from 'rxjs/operators';


@Injectable({ providedIn: 'root' })
export class CompanyStore{
  companyService = inject(CompanyService);
  companies = signal<Company[]>([])
  industries = signal<Industry[]>([])
  showCreateCompanyDialog = signal(false)
  notificationService = inject(NotificationService);


  load = () => {
    this.companyService.list().subscribe(companies => {
      this.companies.set(companies);
    })
    this.companyService.listIndustries().subscribe(industries => {
      this.industries.set(industries);
    })
  }

  createCompany(company: CreateCompanyDto) {
    return this.companyService.createCompany(company).pipe(tap(
      response=> {
        if(response?.data){
          this.companies.update(c=> [response.data as Company, ...c]);
        }

        this.showCreateCompanyDialog.set(false);
      }));
  }

}
