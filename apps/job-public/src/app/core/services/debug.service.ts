import {Injectable, signal, effect} from '@angular/core';
import {environment} from '../../../environments/environment';

export interface ActivityEntry {
  method: string;
  path: string;
  status: number;
  duration: number;
  traceId: string;
  timestamp: Date;
}

const OPEN_KEY = 'debug-sidebar-open';
const ENTRIES_KEY = 'debug-entries';
const MAX_ENTRIES = 50;

@Injectable({providedIn: 'root'})
export class DebugService {
  readonly entries = signal<ActivityEntry[]>(this.loadEntries());
  readonly isOpen = signal(this.getStoredState());

  constructor() {
    effect(() => {
      if (typeof window !== 'undefined') {
        localStorage.setItem(OPEN_KEY, this.isOpen() ? 'true' : 'false');
      }
    });

    effect(() => {
      if (typeof window !== 'undefined') {
        const raw = this.entries().map(e => ({...e, timestamp: e.timestamp.getTime()}));
        sessionStorage.setItem(ENTRIES_KEY, JSON.stringify(raw));
      }
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
    return (environment as any).jaegerUrl + traceId;
  }

  seqLink(traceId: string): string {
    return (environment as any).seqUrl?.replace('{traceId}', traceId) ?? '';
  }

  grafanaLink(traceId: string): string {
    return (environment as any).grafanaUrl + traceId;
  }

  private getStoredState(): boolean {
    if (typeof window === 'undefined') return false;
    return localStorage.getItem(OPEN_KEY) === 'true';
  }

  private loadEntries(): ActivityEntry[] {
    if (typeof window === 'undefined') return [];
    try {
      const raw = sessionStorage.getItem(ENTRIES_KEY);
      if (!raw) return [];
      return JSON.parse(raw).map((e: any) => ({...e, timestamp: new Date(e.timestamp)}));
    } catch {
      return [];
    }
  }
}
