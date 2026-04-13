import {computed, Injectable, signal, effect} from '@angular/core';
import {environment} from '../../../environments/environment';

export interface ActivityEntry {
  method: string;
  path: string;
  status: number;
  duration: number;
  traceId: string;
  timestamp: Date;
}

const STORAGE_KEY = 'debug-sidebar-open';
const MAX_ENTRIES = 50;

@Injectable({providedIn: 'root'})
export class DebugService {
  readonly entries = signal<ActivityEntry[]>([]);
  readonly isOpen = signal(this.getStoredState());

  constructor() {
    effect(() => {
      localStorage.setItem(STORAGE_KEY, this.isOpen() ? 'true' : 'false');
    });
  }

  toggle(): void {
    this.isOpen.update(v => !v);
  }

  push(entry: ActivityEntry): void {
    this.entries.update(list => [entry, ...list].slice(0, MAX_ENTRIES));
  }

  clear(): void {
    this.entries.set([]);
  }

  jaegerLink(traceId: string): string {
    return environment.jaegerUrl + traceId;
  }

  seqLink(traceId: string): string {
    return (environment as any).seqUrl?.replace('{traceId}', traceId) ?? '';
  }

  grafanaLink(traceId: string): string {
    return environment.grafanaUrl + traceId;
  }

  private getStoredState(): boolean {
    return false; // Always start closed
  }
}
