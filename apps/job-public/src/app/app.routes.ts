import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { AuthCallbackComponent } from './shared/auth-callback/auth-callback';

export const routes: Routes = [
  { path: '', title: 'Home', loadComponent: () => import('./features/home/home/home').then((m) => m.Home) },
  { path: 'jobs', title: 'Jobs', loadComponent: () => import('./features/jobs/jobs/jobs').then((m) => m.Jobs) },
  {
    path: 'jobs/:id',
    title: 'Job Details',
    loadComponent: () => import('./features/jobs/job-detail/job-detail').then((m) => m.JobDetail),
  },
  {
    path: 'companies',
    title: 'Companies',
    loadComponent: () =>
      import('./features/companies/companies/companies').then((m) => m.Companies),
  },
  {
    path: 'companies/:id',
    title: 'Company Details',
    loadComponent: () =>
      import('./features/companies/company-detail/company-detail').then((m) => m.CompanyDetail),
  },
  {
    path: 'apply/:jobId',
    title: 'Apply',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/application/application/application').then((m) => m.Application),
  },
  {
    path: 'profile',
    title: 'Profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/profile/profile').then((m) => m.Profile),
  },
  {
    path: 'applications',
    title: 'My Applications',
    canActivate: [authGuard],
    loadComponent: () => import('./features/applications/applications').then((m) => m.Applications),
  },
  { path: '**', redirectTo: '' },
];
