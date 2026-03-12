import { Routes } from '@angular/router';
import { AutoLoginPartialRoutesGuard } from 'angular-auth-oidc-client';
import {AuthCallbackComponent} from './shared/auth-callback/auth-callback';
export const routes: Routes = [
  {
    path: '',
    canActivate: [AutoLoginPartialRoutesGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', title: 'Dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'jobs', title: 'Jobs', loadChildren: () => import('./features/jobs/jobs.routes').then(m => m.JOB_ROUTES) },
      { path: 'companies', title: 'Companies', loadChildren: () => import('./features/companies/companies.routes').then(m => m.COMPANY_ROUTES) },
      { path: 'applications', title: 'Applications', loadChildren: () => import('./features/applications/applications.routes').then(m => m.APPLICATION_ROUTES) },
      { path: 'access', title: 'Access Control', loadChildren:()=> import('./features/access/access.routes').then(m => m.ACCESS_ROUTES) },
      { path: 'settings', title: 'Settings', loadChildren:()=> import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES) },
    ],
  },
  // Fallback
  { path: '**', redirectTo: '' },

];
