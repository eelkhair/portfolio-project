import {Route} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const SETTINGS_ROUTES: Route[] = [
  {
    path:'app',
    canActivate: [roleGuard],
    data: {roles: ['platform-admin']},
    loadComponent: () => import('./settings').then(c=>c.Settings)
  },
  {
    path:'ai-provider',
    canActivate: [roleGuard],
    data: {roles: ['platform-admin']},
    loadComponent: () => import('./ai-provider/ai-provider').then(c=>c.AiProvider)
  },
  {
    path:'application-mode',
    canActivate: [roleGuard],
    data: {roles: ['platform-admin']},
    loadComponent: () => import('./application-mode/application-mode').then(c=>c.ApplicationMode)
  }
]
