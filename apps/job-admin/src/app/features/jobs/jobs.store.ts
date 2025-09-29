import {inject, Injectable, signal} from '@angular/core';
import {Company} from '../../core/types/models/Company';
import {CompanyService} from '../../core/services/company.service';

@Injectable({ providedIn: 'root' })
export class JobsStore {
  companyService = inject(CompanyService);
  company = signal<Company|undefined>(undefined);
  companiesList = signal<Company[]>([]);

  populateCompanies() {
      this.companyService.list().subscribe(companies => {
        if(companies.data){
          if(companies.data.length == 1){
            this.company.set(companies.data[0])
          }else{
            this.companiesList.set(companies.data);
          }

        }
      });
  }
}
