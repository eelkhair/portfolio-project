import {Component, effect, inject, OnDestroy, signal} from '@angular/core';
import {CompanySelection} from '../../shared/companies/company-selection/company-selection';
import {JobsStore} from './jobs.store';
import {ColDef, themeQuartz} from 'ag-grid-community';
import {AgButton} from '../../shared/ag-button/ag-button';
import {AgGridAngular} from 'ag-grid-angular';

@Component({
  selector: 'app-jobs',
  imports: [
    CompanySelection,
    AgGridAngular
  ],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css'
})
export class Jobs{
  store = inject(JobsStore);
  theme = themeQuartz;

  colDefs: ColDef[] = [
    {
      field: 'uId', autoHeight: true,
      width: 100
    },
    { field: 'name' },
    { field: 'website' },
    { field: 'email' },
    { field: 'status'},
    {
      field: 'createdAt',
      valueFormatter: ({ value }) =>
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
      valueFormatter: ({ value }) =>
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
      if(this.store.selectedCompany()) {
        this.store.loadJobs();
      }
    });
  }
}
