import {Routes} from '@angular/router';

export const JOB_ROUTES: Routes = [
  { path: '', title: 'Jobs', loadComponent: () => import('./jobs').then(c=>c.Jobs) },
  { path: 'new', title: 'New Job', loadComponent: () => import('./job-upsert/job-upsert').then(c=>c.JobUpsert) },
  { path: 'new/:draftId', title: 'New Job from Draft', loadComponent: () => import('./job-upsert/job-upsert').then(c=>c.JobUpsert) },
  { path: 'drafts', title: 'Drafts', loadComponent: () => import('./job-drafts/job-drafts').then(c=>c.JobDrafts) },
  { path: ':id', title: 'Job Details', loadComponent: () => import('./job-detail/job-detail').then(c=>c.JobDetail) },
]
