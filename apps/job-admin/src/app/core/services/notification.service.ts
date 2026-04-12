import { inject, Injectable } from '@angular/core';
import { MessageService } from 'primeng/api';
import { ArchitecturePopupService } from '../../shared/architecture-popup/architecture-popup.service';
import { DebugService } from './debug.service';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private messageService = inject(MessageService);
  private architecturePopup = inject(ArchitecturePopupService);
  private debug = inject(DebugService);

  success(summary: string, detail: string) {
    this.messageService.add({ severity: 'success', summary, detail });
  }

  successWithArchitecture(summary: string, detail: string, eventType: string) {
    // Capture trace ID immediately — before navigation triggers new requests
    const traceId = this.debug.entries()[0]?.traceId ?? null;
    const duration = this.debug.entries()[0]?.duration ?? null;
    this.messageService.add({ severity: 'success', summary, detail });
    this.architecturePopup.show(eventType, traceId, duration);
  }

  info(summary: string, detail: string) {
    this.messageService.add({ severity: 'info', summary, detail });
  }

  warn(summary: string, detail: string) {
    this.messageService.add({ severity: 'warn', summary, detail });
  }

  error(summary: string, detail: string) {
    this.messageService.add({ severity: 'error', summary, detail });
  }
}
