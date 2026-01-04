import {Routes} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const COMPANY_ROUTES: Routes = [
  {
    path:'',
    canActivate: [roleGuard],
    data: {roles: ['admin']},
    loadComponent: () => import('./company-list/companies').then(c=>c.Companies)
  },
  {
    path:':id',
    canActivate: [roleGuard],
    data: {roles: ['admin']},
    loadComponent: () => import('./company-details/company-details').then(c=>c.CompanyDetails)
  },
]
