import {Routes} from '@angular/router';

export const JOB_ROUTES: Routes = [
  { path: '', loadComponent: () => import('./jobs').then(c=>c.Jobs) },
]
