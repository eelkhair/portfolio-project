import {Component, inject, OnInit} from '@angular/core';
import {CompanyStore} from '../../company.store';
import {TableModule} from 'primeng/table';
import {DatePipe} from '@angular/common';

@Component({
  selector: 'app-org',
  imports: [
    TableModule,
    DatePipe
  ],
  templateUrl: './companies.html',
  styleUrl: './companies.css'
})
export class Companies implements OnInit {
  store = inject(CompanyStore);
  companies = this.store.companies;

  ngOnInit() {
    this.store.load()
  }
}
