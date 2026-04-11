import {Component, computed, inject, signal} from '@angular/core';
import {ActivityEntry, DebugService} from '../../core/services/debug.service';

export interface EntryGroup {
  traceId: string;
  entries: ActivityEntry[];
  latest: ActivityEntry;
  totalDuration: number;
}

@Component({
  selector: 'app-debug-sidebar',
  templateUrl: './debug-sidebar.html',
  styleUrl: './debug-sidebar.css',
})
export class DebugSidebar {
  debug = inject(DebugService);
  expandedTraces = signal(new Set<string>());

  groupedEntries = computed<EntryGroup[]>(() => {
    const entries = this.debug.entries();
    const map = new Map<string, ActivityEntry[]>();
    const order: string[] = [];

    for (const entry of entries) {
      const existing = map.get(entry.traceId);
      if (existing) {
        existing.push(entry);
      } else {
        map.set(entry.traceId, [entry]);
        order.push(entry.traceId);
      }
    }

    return order.map(traceId => {
      const group = map.get(traceId)!;
      const totalDuration = group.reduce((sum, e) => sum + e.duration, 0);
      return {traceId, entries: group, latest: group[group.length - 1], totalDuration};
    });
  });

  toggleGroup(traceId: string): void {
    this.expandedTraces.update(set => {
      const next = new Set(set);
      if (next.has(traceId)) next.delete(traceId);
      else next.add(traceId);
      return next;
    });
  }

  methodClass(method: string): string {
    switch (method) {
      case 'GET': return 'method-get';
      case 'POST': return 'method-post';
      case 'PUT': return 'method-put';
      case 'DELETE': return 'method-delete';
      default: return '';
    }
  }

  statusClass(status: number): string {
    if (status >= 500) return 'status-error';
    if (status >= 400) return 'status-warn';
    return 'status-ok';
  }

  formatTime(date: Date): string {
    return date.toLocaleTimeString('en-US', {hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit'});
  }

  formatDuration(ms: number): string {
    return ms >= 1000 ? (ms / 1000).toFixed(1) + 's' : ms + 'ms';
  }

  truncatePath(path: string): string {
    return path.length > 35 ? path.slice(0, 35) + '...' : path;
  }

  copyTraceId(traceId: string): void {
    navigator.clipboard.writeText(traceId);
  }
}
