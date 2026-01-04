import {Routes} from '@angular/router';

export const JOB_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./jobs').then(c=>c.Jobs) },
  { path: 'new', loadComponent: () => import('./job-upsert/job-upsert').then(c=>c.JobUpsert) },
  { path: 'new/:draftId', loadComponent: () => import('./job-upsert/job-upsert').then(c=>c.JobUpsert) },
  { path: 'drafts', loadComponent: () => import('./job-drafts/job-drafts').then(c=>c.JobDrafts) },

]
