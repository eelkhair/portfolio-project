import {Routes} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const COMPANY_ROUTES: Routes = [
  {
    path:'',
    title: 'Companies',
    canActivate: [roleGuard],
    data: {groups: ['Admins']},
    loadComponent: () => import('./company-list/companies').then(c=>c.Companies)
  },
  {
    path:':id',
    title: 'Company Details',
    canActivate: [roleGuard],
    data: {groups: ['Admins']},
    loadComponent: () => import('./company-details/company-details').then(c=>c.CompanyDetails)
  },
]
