import {Component, inject} from '@angular/core';
import {DebugService} from '../../core/services/debug.service';
import {DatePipe} from '@angular/common';

@Component({
  selector: 'app-debug-sidebar',
  imports: [DatePipe],
  templateUrl: './debug-sidebar.html',
  styleUrl: './debug-sidebar.css',
})
export class DebugSidebar {
  debug = inject(DebugService);

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
    return date.toLocaleTimeString('en-US', { hour12: false, hour: '2-digit', minute: '2-digit', second: '2-digit' });
  }

  truncatePath(path: string): string {
    return path.length > 35 ? path.slice(0, 35) + '...' : path;
  }

  copyTraceId(traceId: string): void {
    navigator.clipboard.writeText(traceId);
  }
}
