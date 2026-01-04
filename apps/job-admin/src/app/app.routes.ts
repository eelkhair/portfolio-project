import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';
export const routes: Routes = [
  {
    path: '',
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then(m => m.Dashboard) },
      { path: 'jobs', loadChildren: () => import('./features/jobs/jobs.routes').then(m => m.JOB_ROUTES) },
      { path: 'companies', loadChildren: () => import('./features/companies/companies.routes').then(m => m.COMPANY_ROUTES) },
      { path: 'applications', loadChildren: () => import('./features/applications/applications.routes').then(m => m.APPLICATION_ROUTES) },
      { path: 'access', loadChildren:()=> import('./features/access/access.routes').then(m => m.ACCESS_ROUTES) },
      { path: 'settings', loadChildren:()=> import('./features/settings/settings.routes').then(m => m.SETTINGS_ROUTES) },
    ],
  },
  // Fallback
  { path: '**', redirectTo: '' },

];
