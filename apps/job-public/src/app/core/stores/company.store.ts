import { Injectable, signal } from '@angular/core';
import { MockDataService } from '../services/mock-data.service';
import { Company } from '../types/company.type';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class CompanyStore {
  readonly companies = signal<Company[]>([]);
  readonly currentCompany = signal<Company | undefined>(undefined);
  readonly companyJobs = signal<Job[]>([]);
  readonly loading = signal(false);

  constructor(private dataService: MockDataService) {}

  loadCompanies(): void {
    this.loading.set(true);
    this.dataService.getCompanies().subscribe((companies) => {
      this.companies.set(companies);
      this.loading.set(false);
    });
  }

  loadCompany(id: string): void {
    this.loading.set(true);
    this.currentCompany.set(undefined);
    this.companyJobs.set([]);
    this.dataService.getCompanyById(id).subscribe((company) => {
      this.currentCompany.set(company);
      this.loading.set(false);
      if (company) {
        this.dataService.getJobsByCompany(company.id).subscribe((jobs) => {
          this.companyJobs.set(jobs);
        });
      }
    });
  }
}
