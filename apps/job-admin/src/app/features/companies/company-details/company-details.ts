import {Component, inject, OnInit, input} from '@angular/core';
import {CompanyStore} from '../company.store';

@Component({
  selector: 'app-details',
  imports: [],
  templateUrl: './company-details.html',
  styleUrl: './company-details.css'
})
export class CompanyDetails implements OnInit {
  store = inject(CompanyStore);
  id = input.required<string>();

  ngOnInit(): void {
    if(!this.store.selectedCompany()){
      this.store.loadCompany(this.id())
    }
  }
}
