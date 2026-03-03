import { Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Company } from '../../../core/types/company.type';

@Component({
  selector: 'app-company-card',
  imports: [RouterLink],
  templateUrl: './company-card.html',
})
export class CompanyCard {
  company = input.required<Company>();
}
