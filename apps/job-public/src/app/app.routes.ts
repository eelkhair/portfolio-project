import { Routes } from '@angular/router';
import { AuthGuard } from '@auth0/auth0-angular';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./features/home/home').then((m) => m.Home) },
  { path: 'jobs', loadComponent: () => import('./features/jobs/jobs').then((m) => m.Jobs) },
  {
    path: 'jobs/:id',
    loadComponent: () => import('./features/jobs/job-detail').then((m) => m.JobDetail),
  },
  {
    path: 'companies',
    loadComponent: () => import('./features/companies/companies').then((m) => m.Companies),
  },
  {
    path: 'companies/:id',
    loadComponent: () =>
      import('./features/companies/company-detail').then((m) => m.CompanyDetail),
  },
  {
    path: 'apply/:jobId',
    canActivate: [AuthGuard],
    loadComponent: () =>
      import('./features/application/application').then((m) => m.Application),
  },
  { path: '**', redirectTo: '' },
];
