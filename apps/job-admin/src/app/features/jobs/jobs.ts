import {Component, effect, inject} from '@angular/core';
import {CompanySelection} from '../../shared/companies/company-selection/company-selection';
import {JobsStore} from './jobs.store';
import {ColDef} from 'ag-grid-community';
import {AgGridAngular} from 'ag-grid-angular';
import {AgButton} from '../../shared/ag-button/ag-button';
import {Button} from 'primeng/button';
import {RouterLink} from '@angular/router';
import {ThemeService} from '../../core/services/theme.service';

@Component({
  selector: 'app-jobs',
  imports: [
    CompanySelection,
    AgGridAngular,
    Button,
    RouterLink
  ],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css'
})
export class Jobs {
  store = inject(JobsStore);
  themeService = inject(ThemeService);

  defaultColDef: ColDef = { filter: true };
  colDefs: ColDef[] = [
    {
      field: 'uId', autoHeight: true,
      cellRenderer: AgButton,
      width: 100,
      cellRendererParams: (e: any) => {
        return {
          click: () => {
            alert(e.value);
          }
        }
      }
    },
    {field: 'title'},
    {field: 'location'},
    {
      field: 'createdAt',
      valueFormatter: ({value}) =>
        value
          ? new Date(value).toLocaleString(undefined, {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
          })
          : '',
      filter: 'agDateColumnFilter'
    },
    {
      field: 'updatedAt',
      valueFormatter: ({value}) =>
        value
          ? new Date(value).toLocaleString(undefined, {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit',
            second: '2-digit',
            hour12: true
          })
          : '',
      filter: 'agDateColumnFilter'
    },
  ];

  constructor() {
    effect(() => {
      if (this.store.selectedCompany()) {
        this.store.loadJobs();
      }
    });
  }
}
