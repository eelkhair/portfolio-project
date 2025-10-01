import {computed, inject, Injectable, signal} from '@angular/core';
import {JobService} from '../../core/services/job.service';
import {CompanySelectionStore} from '../../shared/companies/company-selection/company-selection.store';
import {ApiResponse} from '../../core/types/Dtos/ApiResponse';
import {Job} from '../../core/types/models/Job';

@Injectable({ providedIn: 'root' })
export class JobsStore {
  companySelectionStore = inject(CompanySelectionStore);
  selectedCompany = this.companySelectionStore.selectedCompany;
  private jobService = inject(JobService);
  jobs = signal<Job[]>([])

  loadJobs(){
    const selectedCompany = this.selectedCompany();
    if(selectedCompany){
      this.jobService.list(selectedCompany.uId).subscribe({
        next: response => {
          this.jobs.set(response.data!)
        }
      })
    }
    return undefined;
  }



}
