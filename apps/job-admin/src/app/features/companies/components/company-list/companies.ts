import {Component, inject, OnInit} from '@angular/core';
import {CompanyStore} from '../../company.store';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, themeQuartz} from 'ag-grid-community';
import {Button} from 'primeng/button';
import {CompanyCreate} from '../company-create/company-create';

@Component({
  selector: 'app-org',
  imports: [

    AgGridAngular,
    Button,
    CompanyCreate
  ],
  templateUrl: './companies.html',
  styleUrl: './companies.css'
})
export class Companies implements OnInit {
  store = inject(CompanyStore);
  companies = this.store.companies;
  theme = themeQuartz; // new theming API
  colDefs: ColDef[] = [
    { field: 'uId'

    },
    { field: 'name' },
    { field: 'eeo' },
    { field: 'about' },
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

  ngOnInit(): void {
    this.store.load();
  }
}
