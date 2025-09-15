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
  selectedCompany = signal<Company|undefined>(undefined);
  notificationService = inject(NotificationService);


  load = () => {
    this.loadCompanies().subscribe(response => {
      this.companies.set(response.data!);
    })
    this.loadIndustries().subscribe(response => {
      this.industries.set(response.data!);
    })
  }
  loadCompanies = () => {
    return this.companyService.list()
  }
  loadIndustries = () => {
    return this.companyService.listIndustries()
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

  selectCompany(id: string) {
    this.selectedCompany.set(this.companies().find(c=>c.uId === id));
  }

  loadCompany(id: string) {
    this.loadIndustries().subscribe(response => {
      this.industries.set(response.data!);
    })
    this.loadCompanies().subscribe(response => {
        this.companies.set(response.data!);
        this.selectCompany(id);

    })
  }
}
