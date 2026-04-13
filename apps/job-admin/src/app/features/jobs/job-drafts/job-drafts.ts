import {Component, effect, inject} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef} from 'ag-grid-community';
import {AgButton} from '../../../shared/ag-button/ag-button';
import {AgDeleteButton} from '../../../shared/ag-delete-button/ag-delete-button';
import {Router} from '@angular/router';
import {ThemeService} from '../../../core/services/theme.service';
import {AgSetFilter} from '../../../shared/ag-set-filter/ag-set-filter';
import {JOB_TYPE_LABELS} from '../../../core/types/Dtos/CreateJobRequest';
import {ConfirmationService} from 'primeng/api';
import {ConfirmDialog} from 'primeng/confirmdialog';
import {Draft} from '../../../core/types/Dtos/draft';

@Component({
  selector: 'app-job-drafts',
  imports: [
    CompanySelection,
    AgGridAngular,
    ConfirmDialog
  ],
  providers: [ConfirmationService],
  templateUrl: './job-drafts.html',
  styleUrl: './job-drafts.css'
})
export class JobDrafts{
  store = inject(JobsStore);
  router = inject(Router);
  themeService = inject(ThemeService);
  private confirmationService = inject(ConfirmationService);
  defaultColDef: ColDef = { filter: true };
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
    {
      field: 'jobType',
      filter: AgSetFilter,
      filterParams: { labelMap: JOB_TYPE_LABELS },
      valueFormatter: ({ value }) => JOB_TYPE_LABELS[value as keyof typeof JOB_TYPE_LABELS] ?? value,
    },
    { field: 'salaryRange' },
    {
      headerName: '',
      width: 80,
      filter: false,
      sortable: false,
      cellRenderer: AgDeleteButton,
      cellRendererParams: (e: any) => ({
        click: () => this.confirmDelete(e.data)
      })
    }
  ]
  constructor() {
    effect(()=>{
      const company = this.store.selectedCompany();
      if (company){
        this.store.loadDrafts(company.uId).subscribe();
      }
    })
  }

  confirmDelete(draft: Draft) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete draft "${draft.title}"? This action cannot be undone.`,
      header: 'Delete Draft',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Delete',
      rejectLabel: 'Cancel',
      acceptIcon: 'pi pi-trash',
      rejectIcon: 'pi pi-times',
      acceptButtonStyleClass: 'p-button-danger',
      rejectButtonStyleClass: 'p-button-outlined',
      accept: () => {
        this.store.deleteDraft(draft.id!).subscribe();
      }
    });
  }
}
