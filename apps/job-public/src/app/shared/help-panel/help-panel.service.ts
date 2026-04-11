import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class HelpPanelService {
  readonly visible = signal(false);
}
