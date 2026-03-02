import { ErrorHandler, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformServer } from '@angular/common';
import { trace, SpanKind, SpanStatusCode } from '@opentelemetry/api';

export class TracingErrorHandler implements ErrorHandler {
  private readonly platformId = inject(PLATFORM_ID);

  handleError(error: unknown): void {
    // Always log to console
    console.error(error);

    if (isPlatformServer(this.platformId)) {
      return;
    }

    const tracer = trace.getTracer('public-fe');
    const span = tracer.startSpan('unhandled.error', {
      kind: SpanKind.INTERNAL,
      attributes: {
        'error.type': this.getErrorType(error),
        'error.message': this.getMessage(error),
      },
    });

    if (error instanceof Error) {
      span.recordException(error);
    }

    span.setStatus({ code: SpanStatusCode.ERROR, message: this.getMessage(error) });
    span.end();
  }

  private getErrorType(error: unknown): string {
    if (error instanceof Error) {
      return error.constructor.name;
    }
    return typeof error;
  }

  private getMessage(error: unknown): string {
    if (error instanceof Error) {
      return error.message;
    }
    return String(error);
  }
}
