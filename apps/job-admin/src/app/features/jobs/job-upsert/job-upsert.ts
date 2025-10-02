import {Component, inject} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {InputText} from 'primeng/inputtext';
import {Textarea} from 'primeng/textarea';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-job-upsert',
  imports: [
    CompanySelection,
    InputText,
    Textarea,
    Button
  ],
  templateUrl: './job-upsert.html',
  styleUrl: './job-upsert.css'
})
export class JobUpsert {
  store = inject(JobsStore)
}
