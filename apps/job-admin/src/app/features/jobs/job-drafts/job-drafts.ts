import {Component, effect, inject} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {ApiResponse} from '../../../core/types/Dtos/ApiResponse';
import {Draft} from '../../../core/types/Dtos/draft';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, themeQuartz} from 'ag-grid-community';
import {AgButton} from '../../../shared/ag-button/ag-button';

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
  colDefs: ColDef[] = [
    { field: 'id', autoHeight: true,
      cellRenderer: AgButton,
      width:100,
      cellRendererParams: (e:any)=>{
        return { click:()=>{
            alert(e.value);
            //  void this.router.navigateByUrl('/companies/'+ e.value);
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
        this.store.loadDrafts(this.store.selectedCompany()?.uId!).subscribe((drafts: ApiResponse<Draft[]>) => {
          console.log(drafts);
        });
      }
    })

  }

  protected readonly theme = themeQuartz;
}
