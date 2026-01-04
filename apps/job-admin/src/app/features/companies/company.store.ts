import { effect, inject, Injectable, signal } from '@angular/core';
import { CompanyService } from '../../core/services/company.service';
import { Company } from '../../core/types/models/Company';
import { Industry } from '../../core/types/models/Industry';
import { CreateCompanyDto } from '../../core/types/Dtos/CreateCompanyDto';
import { NotificationService } from '../../core/services/notification.service';
import { RealtimeNotificationsService } from '../../core/services/realtime-notifications.service';
import { tap, finalize } from 'rxjs/operators';
import {FeatureFlagsService} from '../../core/services/feature-flags.service';

@Injectable({ providedIn: 'root' })
export class CompanyStore {

  private readonly companyService = inject(CompanyService);
  readonly notificationService = inject(NotificationService);
  private readonly featureFlagService = inject(FeatureFlagsService);
  private readonly realtimeNotificationService = inject(RealtimeNotificationsService);

  // ---- state ----
  readonly companies = signal<Company[]>([]);
  readonly industries = signal<Industry[]>([]);
  readonly selectedCompany = signal<Company | undefined>(undefined);

  readonly showCreateCompanyDialog = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  initial = true;
  constructor() {
    // Realtime company activation projection
    effect(() => {
      const activated = this.realtimeNotificationService.companyActivated();
      if (!activated) return;
     console.log("Activated company", activated);
      this.companies.update(list =>
        list.map(c =>
          c.uId === activated.companyUId
            ? { ...c, status: 'Active' }
            : c
        )
      );
    });
    effect(()=>{
      if(this.featureFlagService.featureFlags()&& this.initial){
        this.loadIndustries().subscribe();
        this.loadCompanies().subscribe();
        this.initial=false;
      }
    })
  }

  // ---- public API ----



  loadCompany(id: string) {
    this.loadIndustries().subscribe();

    this.loadCompanies().pipe(
      tap(() => this.selectCompany(id))
    ).subscribe();
  }

  selectCompany(id: string) {
    this.selectedCompany.set(
      this.companies().find(c => c.uId === id)
    );
  }

  createCompany(dto: CreateCompanyDto) {
    this.loading.set(true);
    this.error.set(null);

    return this.companyService.createCompany(dto).pipe(
      tap({
        next: response => {
          if (response?.data) {
            this.companies.update(list => [response.data!, ...list]);
          }
          console.log(this.companies());
          this.showCreateCompanyDialog.set(false);
          this.notificationService.success(
            'Success',
            'Company created successfully'
          );
        },
        error: err => {
          this.error.set(err.message ?? 'Failed to create company');
          this.notificationService.error(
            'Error',
            'Failed to create company'
          );
        }
      }),
      finalize(() => this.loading.set(false))
    );
  }

  // ---- private helpers ----

  private loadCompanies() {
    this.loading.set(true);
    this.error.set(null);

    return this.companyService.listCompanies().pipe(
      tap({
        next: response => {
          this.companies.set(response.data ?? [])
        },
        error: err =>
          this.error.set(err.message ?? 'Failed to load companies')
      }),
      finalize(() => this.loading.set(false))
    );
  }

  private loadIndustries() {
    return this.companyService.listIndustries().pipe(
      tap(response =>{
        this.industries.set(response.data ?? [])
        console.log(this.industries())
      } )
    );
  }
}
