export interface AiNotificationDto {
  traceParent?: string;
  traceState?: string;
  type: string;
  title: string;
  entityId: string;
  entityType: string;
  correlationId?: string;
  timestamp: string;
  metadata?: Record<string, any>;
}
