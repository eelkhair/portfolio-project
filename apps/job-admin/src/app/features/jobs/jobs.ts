import {Component, inject, OnInit} from '@angular/core';
import {JobsStore} from './jobs.store';
import {ReactiveFormsModule} from '@angular/forms';
import {Select} from 'primeng/select';

@Component({
  selector: 'app-jobs',
  imports: [
    ReactiveFormsModule,
    Select
  ],
  templateUrl: './jobs.html',
  styleUrl: './jobs.css'
})
export class Jobs implements OnInit {
  store = inject(JobsStore);
  ngOnInit() {
    this.store.populateCompanies();
  }
}
