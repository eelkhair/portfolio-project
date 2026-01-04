import {Component, effect, inject} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, themeQuartz} from 'ag-grid-community';
import {AgButton} from '../../../shared/ag-button/ag-button';
import {Router} from '@angular/router';

@Component({
  selector: 'app-job-drafts',
  imports: [
    CompanySelection,
    AgGridAngular
  ],
  templateUrl: './job-drafts.html',
  styleUrl: './job-drafts.css'
})
export class JobDrafts{
  store = inject(JobsStore);
  router= inject(Router);
  colDefs: ColDef[] = [
    { field: 'id', autoHeight: true,
      cellRenderer: AgButton,
      width:100,
      cellRendererParams: (e:any)=>{
        return { click:()=>{
              void this.router.navigateByUrl('/jobs/new/'+ e.value);
          }
        }
      }},
    { field: 'title' },
    { field: 'aboutRole' },
    { field: 'location' },
    { field: 'jobType' },
    { field: 'salaryRange' },
  ]
  constructor() {
    effect(()=>{
      if (this.store.selectedCompany()){
        this.store.loadDrafts(this.store.selectedCompany()?.uId!).subscribe();
      }
    })

  }

  protected readonly theme = themeQuartz;
}
