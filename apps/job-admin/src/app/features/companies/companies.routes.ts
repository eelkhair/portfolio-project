import {Routes} from '@angular/router';

export const COMPANY_ROUTES: Routes = [
  {path:'', loadComponent: () => import('./components/company-list/companies').then(c=>c.Companies) },
]
