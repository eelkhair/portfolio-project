import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { JobCard } from '../../jobs/job-card/job-card';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { CompanyStore } from '../../../core/stores/company.store';

@Component({
  selector: 'app-company-detail',
  imports: [RouterLink, JobCard, LoadingSpinner, DatePipe],
  templateUrl: './company-detail.html',
})
export class CompanyDetail implements OnInit {
  protected readonly store = inject(CompanyStore);
  private readonly route = inject(ActivatedRoute);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.store.loadCompany(id);
    }
  }
}
