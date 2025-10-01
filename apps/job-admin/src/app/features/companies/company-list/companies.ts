import {Component, inject, OnInit} from '@angular/core';
import {CompanyStore} from '../company.store';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, themeQuartz} from 'ag-grid-community';
import {Button} from 'primeng/button';
import {CompanyCreate} from '../company-create/company-create';
import {AgButton} from '../../../shared/ag-button/ag-button';
import {Router} from '@angular/router';

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
  theme = themeQuartz;
  router = inject(Router);
  colDefs: ColDef[] = [
    { field: 'uId', autoHeight: true,
      cellRenderer: AgButton,
      width:100,
      cellRendererParams: (e:any)=>{
        return { click:()=>{
            this.store.selectCompany(e.value);
            void this.router.navigateByUrl('/companies/'+ e.value);
          }
        }
      }},
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

  ngOnInit(): void {
    this.store.load();
  }
}
