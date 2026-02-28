import { inject, Injectable, signal } from '@angular/core';
import { ApiService } from '../services/api.service';
import { Company } from '../types/company.type';
import { Job } from '../types/job.type';

@Injectable({ providedIn: 'root' })
export class CompanyStore {
  private readonly api = inject(ApiService);

  readonly companies = signal<Company[]>([]);
  readonly currentCompany = signal<Company | undefined>(undefined);
  readonly companyJobs = signal<Job[]>([]);
  readonly loading = signal(false);

  loadCompanies(): void {
    this.loading.set(true);
    this.api.getCompanies().subscribe((companies) => {
      this.companies.set(companies);
      this.loading.set(false);
    });
  }

  loadCompany(id: string): void {
    this.loading.set(true);
    this.currentCompany.set(undefined);
    this.companyJobs.set([]);
    this.api.getCompanyById(id).subscribe((company) => {
      this.currentCompany.set(company);
      this.loading.set(false);
      if (company) {
        this.api.getCompanyJobs(company.id).subscribe((jobs) => {
          this.companyJobs.set(jobs);
        });
      }
    });
  }
}
