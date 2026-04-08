import {Routes} from '@angular/router';

export const APPLICATION_ROUTES: Routes = [
  { path: '', title: 'Pipeline', loadComponent: () => import('./pipeline/pipeline').then(c => c.Pipeline) },
  { path: 'reviews', title: 'Reviews', loadComponent: () => import('./reviews/reviews').then(c => c.Reviews) },
]
