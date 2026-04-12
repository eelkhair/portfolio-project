import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ArchitecturePopupService {
  readonly visible = signal(false);
  readonly eventType = signal<string | null>(null);
  readonly traceId = signal<string | null>(null);
  readonly duration = signal<number | null>(null);

  show(eventType: string, traceId?: string | null, duration?: number | null) {
    this.eventType.set(eventType);
    this.traceId.set(traceId ?? null);
    this.duration.set(duration ?? null);
    this.visible.set(true);
  }

  close() {
    this.visible.set(false);
    this.eventType.set(null);
    this.traceId.set(null);
    this.duration.set(null);
  }
}
