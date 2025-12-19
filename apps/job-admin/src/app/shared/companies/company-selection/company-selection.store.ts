import {inject, Injectable, signal} from '@angular/core';
import {CompanyService} from '../../../core/services/company.service';
import {AccountService} from '../../../core/services/account.service';
import {Company} from '../../../core/types/models/Company';

@Injectable({ providedIn: 'root' })
export class CompanySelectionStore {
  private companyService = inject(CompanyService);
  private accountService = inject(AccountService);
  selectedCompany = signal<Company|undefined>(undefined);
  companiesList = signal<Company[]>([]);

  populateCompanies() {
    this.companyService.listCompanies().subscribe(companies => {
      if(companies.data){
        if(companies.data.length == 1){
          this.selectedCompany.set(companies.data[0])
        }else{
          const org =this.accountService.user()?.["https://eelkhair.net/org"]
          if(org){
            const company = companies.data.find(company => company.uId === org.name)
            if(company){
              this.selectedCompany.set(company)
            }
          }
          this.companiesList.set(companies.data);
        }
      }
    });
  }
}
