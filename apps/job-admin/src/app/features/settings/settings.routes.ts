import {Route} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const SETTINGS_ROUTES: Route[] = [
  {
    path:'app',
    canActivate: [roleGuard],
    data: {roles: ['admin']},
    loadComponent: () => import('./settings').then(c=>c.Settings)
  }
]
