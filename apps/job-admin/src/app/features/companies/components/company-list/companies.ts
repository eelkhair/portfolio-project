import {Component, inject, OnInit} from '@angular/core';
import {CompanyStore} from '../../company.store';
import {AgGridAngular} from 'ag-grid-angular';
import {ColDef, themeQuartz} from 'ag-grid-community';

@Component({
  selector: 'app-org',
  imports: [

    AgGridAngular
  ],
  templateUrl: './companies.html',
  styleUrl: './companies.css'
})
export class Companies implements OnInit {
  store = inject(CompanyStore);
  companies = this.store.companies;

  theme = themeQuartz; // new theming API
  colDefs: ColDef[] = [
    { field: 'uId' },
    { field: 'name' },
    { field: 'eeo' },
    { field: 'about' },
    { field: 'createdAt', type: 'date' },
    { field: 'updatedAt', type: 'date' },

  ];

  ngOnInit(): void {
    this.store.load();
  }
}
