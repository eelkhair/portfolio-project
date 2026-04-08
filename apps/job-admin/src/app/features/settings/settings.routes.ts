import {Route} from '@angular/router';
import {roleGuard} from '../../core/guards/role-guard';

export const SETTINGS_ROUTES: Route[] = [
  {
    path:'app',
    title: 'App Settings',
    canActivate: [roleGuard],
    data: {groups: ['SystemAdmins']},
    loadComponent: () => import('./settings').then(c=>c.Settings)
  },
  {
    path:'ai-provider',
    title: 'AI Provider',
    canActivate: [roleGuard],
    data: {groups: ['SystemAdmins']},
    loadComponent: () => import('./ai-provider/ai-provider').then(c=>c.AiProvider)
  },
  {
    path:'application-mode',
    title: 'Application Mode',
    canActivate: [roleGuard],
    data: {groups: ['SystemAdmins']},
    loadComponent: () => import('./application-mode/application-mode').then(c=>c.ApplicationMode)
  },
  {
    path:'embedding-management',
    title: 'Embedding Management',
    canActivate: [roleGuard],
    data: {groups: ['SystemAdmins']},
    loadComponent: () => import('./embedding-management/embedding-management').then(c=>c.EmbeddingManagement)
  }
]
