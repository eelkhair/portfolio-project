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

  timeAgo(date: Date): string {
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);
    if (seconds < 5) return 'just now';
    if (seconds < 60) return `${seconds}s ago`;
    const minutes = Math.floor(seconds / 60);
    if (minutes < 60) return `${minutes}m ago`;
    return `${Math.floor(minutes / 60)}h ago`;
  }

  truncatePath(path: string): string {
    return path.length > 35 ? path.slice(0, 35) + '...' : path;
  }
}
