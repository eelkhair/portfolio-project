import {Injectable, signal} from '@angular/core';

@Injectable({providedIn: 'root'})
export class GettingStartedService {
  readonly visible = signal(false);
}
