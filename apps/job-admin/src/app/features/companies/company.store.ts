import {effect, inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../core/services/company.service';
import {Company} from '../../core/types/models/Company';
import {Industry} from '../../core/types/models/Industry';
import {CreateCompanyDto} from '../../core/types/Dtos/CreateCompanyDto';
import {NotificationService} from '../../core/services/notification.service';
import {tap} from 'rxjs/operators';
import {RealtimeNotificationsService} from '../../core/services/realtime-notifications.service';
import {FeatureFlagsService} from '../../core/services/feature-flags.service';


@Injectable({ providedIn: 'root' })
export class CompanyStore{
  companyService = inject(CompanyService);
  notificationService = inject(NotificationService);
  featureflagsService = inject(FeatureFlagsService)
  realtimeNotificationService = inject(RealtimeNotificationsService);
  companies = signal<Company[]>([])
  industries = signal<Industry[]>([])
  showCreateCompanyDialog = signal(false)
  selectedCompany = signal<Company|undefined>(undefined);


  constructor() {
    effect(()=>{
      const activated = this.realtimeNotificationService.companyActivated();
      if (!activated) return;
      this.companies.update(list =>
        list.map(c => c.uId === activated.companyUId
          ? { ...c, status: 'Active' }
          : c)
      );
    })
  }
  load = () => {
    this.loadCompanies().subscribe();
    this.loadIndustries().subscribe();
  }

  loadCompanies = () => {
    if(this.featureflagsService.isMonolith()){
      return this.companyService.list().pipe(tap(response=>{
        this.companies.set(response.data!);
      }))
    }else{
      return this.companyService.list().pipe(tap(response=>{
        this.companies.set(response.data!);
      }))
    }
  }
  loadIndustries = () => {
    if(this.featureflagsService.isMonolith()){
      return this.companyService.listIndustries().pipe(tap(response=>{
        this.industries.set(response.data!);
      }))
    }else{
      return this.companyService.listIndustries().pipe(tap(response=>{
        this.industries.set(response.data!);
      }))
    }
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

    this.loadIndustries().subscribe()
    this.loadCompanies().subscribe(() => {
        this.selectCompany(id);
    })
  }
}
