import { ErrorHandler } from '@angular/core';
import { trace, SpanKind, SpanStatusCode } from '@opentelemetry/api';

export class TracingErrorHandler implements ErrorHandler {
  handleError(error: unknown): void {
    console.error(error);

    const tracer = trace.getTracer('admin-fe');
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
