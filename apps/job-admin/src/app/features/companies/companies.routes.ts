import {Routes} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const COMPANY_ROUTES: Routes = [
  {
    path:'',
    canActivate: [roleGuard],
    data: {roles: ['admin']},
    loadComponent: () => import('./components/company-list/companies').then(c=>c.Companies)
  },
]
