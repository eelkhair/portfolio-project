import { Component, effect, ElementRef, inject, signal, ViewChild, ViewEncapsulation } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Drawer } from 'primeng/drawer';
import { Tag } from 'primeng/tag';
import { ProgressSpinner } from 'primeng/progressspinner';
import { ArchitecturePopupService } from './architecture-popup.service';
import { ARCHITECTURE_EVENTS, ArchitectureEvent } from './diagrams';
import { DebugService } from '../../core/services/debug.service';
import { environment } from '../../../environments/environment';
import mermaid from 'mermaid';

@Component({
  selector: 'app-architecture-popup',
  encapsulation: ViewEncapsulation.None,
  imports: [Drawer, Tag, ProgressSpinner],
  template: `
    <p-drawer
      [visible]="popup.visible()"
      (visibleChange)="popup.visible.set($event)"
      position="right"
      [style]="{width: '85vw', maxWidth: '1100px'}"
      (onHide)="onClose()">

      <ng-template #header>
        <div class="flex items-center gap-2">
          <i class="pi pi-sitemap text-primary"></i>
          <span class="font-semibold text-lg">What Just Happened?</span>
        </div>
      </ng-template>

      @if (event()) {
        <div class="flex flex-col gap-4">
          <div>
            <h3 class="text-xl font-bold m-0">{{ event()!.title }}</h3>
            <p class="text-sm text-color-secondary mt-1">{{ event()!.description }}</p>
          </div>

          @if (traceDuration()) {
            <div class="text-sm text-color-secondary">
              <i class="pi pi-clock mr-1"></i> End-to-end: {{ traceDuration() }}ms
            </div>
          }

          <!-- Services involved -->
          <div class="flex flex-wrap gap-1">
            @for (svc of services(); track svc) {
              <p-tag [value]="svc" severity="info" />
            }
          </div>

          <!-- Mermaid diagram -->
          <div class="overflow-auto rounded-lg surface-ground p-3">
            @if (diagramLoading()) {
              <div class="flex justify-center py-8">
                <p-progressSpinner strokeWidth="3" />
              </div>
            }
            <div #diagramContainer></div>
          </div>

          <!-- Trace links -->
          @if (popup.traceId()) {
            <div class="flex gap-3">
              <a class="flex items-center gap-1 text-sm font-semibold no-underline"
                 style="color: #B45309"
                 [href]="jaegerUrl + popup.traceId()"
                 target="_blank" rel="noopener">
                <i class="pi pi-external-link"></i> View in Jaeger
              </a>
              <a class="flex items-center gap-1 text-sm font-semibold no-underline"
                 style="color: #E65100"
                 [href]="grafanaUrl + popup.traceId()"
                 target="_blank" rel="noopener">
                <i class="pi pi-external-link"></i> View in Grafana
              </a>
            </div>
          }
        </div>
      }
    </p-drawer>
  `,
})
export class ArchitecturePopup {
  readonly popup = inject(ArchitecturePopupService);
  private readonly debug = inject(DebugService);
  private readonly http = inject(HttpClient);

  readonly jaegerUrl = environment.jaegerUrl;
  readonly grafanaUrl = environment.grafanaUrl;
  // Fetch traces through the gateway's /jaeger-api/ proxy route
  private readonly jaegerApiUrl = `${environment.gatewayUrl}jaeger-api/api/traces/`;

  @ViewChild('diagramContainer') diagramContainer?: ElementRef;

  readonly event = signal<ArchitectureEvent | null>(null);
  readonly services = signal<string[]>([]);
  readonly diagramLoading = signal(false);
  readonly traceDuration = signal<number | null>(null);

  constructor() {
    mermaid.initialize({ startOnLoad: false, theme: 'dark', themeVariables: { fontSize: '12px' } });

    effect(() => {
      const type = this.popup.eventType();
      if (!type) return;

      const resolved = ARCHITECTURE_EVENTS[type];
      if (resolved) this.event.set(resolved);
      this.diagramLoading.set(true);

      // Auto-fill trace ID from debug service
      if (!this.popup.traceId()) {
        const last = this.debug.entries()[0];
        if (last) {
          this.popup.traceId.set(last.traceId);
          this.popup.duration.set(last.duration);
        }
      }

      // Fetch live trace from Jaeger and build diagram
      const traceId = this.popup.traceId();
      if (traceId) {
        const delay = (type === 'create-job' || type === 'create-company') ? 6000 : 4000;
        setTimeout(() => this.fetchAndRenderTrace(traceId), delay);
      }
    });
  }

  private fetchAttempt = 0;

  private fetchAndRenderTrace(traceId: string) {
    this.diagramLoading.set(true);
    this.fetchAttempt++;
    const attempt = this.fetchAttempt;

    this.http.get<any>(`${this.jaegerApiUrl}${traceId}`).subscribe({
      next: (response) => {
        if (attempt !== this.fetchAttempt) return; // stale
        const trace = response?.data?.[0];
        if (trace) {
          const { diagram, serviceNames, durationMs } = this.buildDiagramFromTrace(trace);
          this.services.set(serviceNames);
          this.traceDuration.set(durationMs);
          this.renderMermaid(diagram);

          // Retry to pick up late-arriving async spans (outbox, embeddings, SignalR)
          const retryDelays = [6000, 14000];
          let lastSpanCount = trace.spans.length;
          for (const retryDelay of retryDelays) {
            setTimeout(() => {
              if (attempt !== this.fetchAttempt) return;
              this.http.get<any>(`${this.jaegerApiUrl}${traceId}`).subscribe({
                next: (retry) => {
                  const retryTrace = retry?.data?.[0];
                  if (retryTrace && retryTrace.spans.length > lastSpanCount) {
                    lastSpanCount = retryTrace.spans.length;
                    const result = this.buildDiagramFromTrace(retryTrace);
                    this.services.set(result.serviceNames);
                    this.traceDuration.set(result.durationMs);
                    this.renderMermaid(result.diagram);
                  }
                }
              });
            }, retryDelay);
          }
        }
        this.diagramLoading.set(false);
      },
      error: () => {
        this.diagramLoading.set(false);
      }
    });
  }

  private buildDiagramFromTrace(trace: any): { diagram: string; serviceNames: string[]; durationMs: number } {
    const processes = trace.processes as Record<string, { serviceName: string }>;
    const spans = (trace.spans as any[]).sort((a, b) => a.startTime - b.startTime);
    const spanMap = new Map(spans.map(s => [s.spanID, s]));

    // Merge Dapr sidecars with their app service (e.g., "admin-api-dapr-cli" → "admin-api")
    const resolveService = (pid: string): string => {
      const name = processes[pid]?.serviceName ?? 'unknown';
      return name.replace(/-dapr-cli$/, '').replace(/-dapr$/, '');
    };

    // Collect unique services in order of appearance
    const serviceOrder: string[] = [];
    const serviceSet = new Set<string>();
    for (const span of spans) {
      const svc = resolveService(span.processID);
      if (!serviceSet.has(svc)) {
        serviceSet.add(svc);
        serviceOrder.push(svc);
      }
    }

    // Only exclude noise — keep everything else
    const isNoise = (span: any): boolean => {
      const op = span.operationName;
      const kind = this.getTag(span, 'span.kind');

      // DB query spans (operation name is the DB name)
      if (this.getTag(span, 'db.system')) return true;

      // Dapr internal proto calls
      if (op.startsWith('/dapr.proto')) return true;

      // Raw HTTP client spans (GET, POST, PUT without a route) — keep server/named ones
      if (kind === 'client' && /^(GET|POST|PUT|DELETE|PATCH)$/.test(op)) return true;

      return false;
    };

    const interestingSpans = spans.filter(s => !isNoise(s));

    // Build sequence diagram
    const lines = ['sequenceDiagram'];

    // Add participants
    const usedServices = new Set<string>();
    for (const span of interestingSpans) {
      usedServices.add(resolveService(span.processID));
      const parentRef = span.references?.find((r: any) => r.refType === 'CHILD_OF');
      if (parentRef && spanMap.has(parentRef.spanID)) {
        usedServices.add(resolveService(spanMap.get(parentRef.spanID)!.processID));
      }
    }

    for (const svc of serviceOrder) {
      if (usedServices.has(svc)) {
        const alias = svc.replace(/[^a-zA-Z0-9]/g, '_');
        lines.push(`participant ${alias} as ${svc}`);
      }
    }

    // Build a map of messaging topics: topic → producer service (pub/sub + SignalR)
    const topicProducers = new Map<string, string>();
    for (const span of interestingSpans) {
      const topic = this.getTag(span, 'messaging.destination.name');
      const kind = this.getTag(span, 'span.kind');
      if (topic && (span.operationName.startsWith('pubsub/') || kind === 'producer')) {
        topicProducers.set(topic, resolveService(span.processID));
      }
    }

    // Find root spans of orphaned subtrees (parent not in trace)
    // These are typically the entry points into services triggered by Dapr pub/sub
    const orphanRoots = new Map<string, any>(); // spanID → span
    for (const span of interestingSpans) {
      const parentRef = span.references?.find((r: any) => r.refType === 'CHILD_OF');
      if (parentRef && !spanMap.has(parentRef.spanID)) {
        orphanRoots.set(span.spanID, span);
      }
    }

    // Build arrows
    const added = new Set<string>();
    const addArrow = (fromSvc: string, toSvc: string, label: string, durationMs: number, async: boolean) => {
      if (fromSvc === toSvc) return;
      const key = `${fromSvc}→${toSvc}:${label}`;
      if (added.has(key)) return;
      added.add(key);

      const fAlias = fromSvc.replace(/[^a-zA-Z0-9]/g, '_');
      const tAlias = toSvc.replace(/[^a-zA-Z0-9]/g, '_');
      usedServices.add(fromSvc);
      usedServices.add(toSvc);

      if (async) {
        lines.push(`${fAlias}-)${tAlias}: ${label}`);
      } else {
        lines.push(`${fAlias}->>+${tAlias}: ${label} (${durationMs}ms)`);
        lines.push(`${tAlias}-->>-${fAlias}: ✓`);
      }
    };

    for (const span of interestingSpans) {
      const childSvc = resolveService(span.processID);
      const parentRef = span.references?.find((r: any) => r.refType === 'CHILD_OF');
      const durationMs = Math.round(span.duration / 1000);

      let label = span.operationName;
      label = label.replace('CallLocal/', '→ ');
      label = label.replace('pubsub/', '📨 ');
      if (label.length > 50) label = label.substring(0, 47) + '...';

      // Case 0: Root span with no parent — find first child in a different service
      if (!parentRef) {
        // This is a root (e.g., admin-fe HTTP POST). Find its server-side child.
        const serverChild = interestingSpans.find(s => {
          const ref = s.references?.find((r: any) => r.refType === 'CHILD_OF');
          return ref?.spanID === span.spanID && resolveService(s.processID) !== childSvc;
        });
        if (serverChild) {
          const serverSvc = resolveService(serverChild.processID);
          addArrow(childSvc, serverSvc, label, durationMs, false);
        }
        continue;
      }

      // Case 1: Valid parent in trace — normal cross-service arrow
      if (parentRef && spanMap.has(parentRef.spanID)) {
        const parentSpan = spanMap.get(parentRef.spanID)!;
        const parentSvc = resolveService(parentSpan.processID);
        const kind = this.getTag(span, 'span.kind');
        const isAsync = kind === 'consumer' || label.includes('📨') || label.includes('signalr');
        addArrow(parentSvc, childSvc, label, durationMs, isAsync);
        continue;
      }

      // Case 2: Orphan root — connect via pub/sub topic or find producing service
      if (orphanRoots.has(span.spanID)) {
        // Check if this span has a topic tag directly
        const topic = this.getTag(span, 'messaging.destination.name');
        if (topic) {
          const producerSvc = topicProducers.get(topic);
          if (producerSvc) {
            addArrow(producerSvc, childSvc, `📨 ${topic}`, durationMs, true);
            continue;
          }
        }

        // No topic tag — this is an app handler triggered by Dapr delivery.
        // Find the pubsub/* span on the same service that contains this span's time range.
        const myStart = span.startTime;
        const pubsubParent = interestingSpans.find(s => {
          if (!s.operationName.startsWith('pubsub/') && !s.operationName.startsWith('📨')) return false;
          const sSvc = resolveService(s.processID);
          if (sSvc !== childSvc) return false;
          return s.startTime <= myStart && (s.startTime + s.duration) >= myStart;
        });

        if (pubsubParent) {
          // Already handled by the pubsub arrow — skip to avoid duplicates
          continue;
        }

        // Last resort: find a producer service by looking at the closest pubsub span before this one
        const closestPubsub = [...interestingSpans]
          .filter(s => s.operationName.startsWith('pubsub/') && s.startTime <= myStart)
          .sort((a, b) => b.startTime - a.startTime)[0];

        if (closestPubsub) {
          const pubTopic = this.getTag(closestPubsub, 'messaging.destination.name') ?? closestPubsub.operationName;
          const producerSvc = resolveService(closestPubsub.processID);
          addArrow(producerSvc, childSvc, label, durationMs, true);
        }
      }
    }

    // Add notes for key internal operations (OpenAI, embeddings, etc.)
    const notesAdded = new Set<string>();
    for (const span of spans) {
      const svc = resolveService(span.processID);
      if (!usedServices.has(svc)) continue;
      const alias = svc.replace(/[^a-zA-Z0-9]/g, '_');
      const op = span.operationName;
      const dMs = Math.round(span.duration / 1000);

      const urlTag = this.getTag(span, 'url.full') ?? '';
      if (urlTag.includes('api.openai.com') && !notesAdded.has(`${svc}:openai:${op}`)) {
        const endpoint = urlTag.includes('chat/completions') ? 'LLM completion' :
                         urlTag.includes('embeddings') ? 'Embedding generation' : 'OpenAI call';
        notesAdded.add(`${svc}:openai:${op}`);
        lines.push(`Note over ${alias}: 🤖 ${endpoint} (${dMs}ms)`);
      }

      if (op === 'text.embedding.batch' && !notesAdded.has(`${svc}:embed`)) {
        const count = this.getTag(span, 'embedding.batch.count') ?? '?';
        notesAdded.add(`${svc}:embed`);
        lines.push(`Note over ${alias}: 📐 ${count}x embeddings (${dMs}ms)`);
      }

      if (op.includes('ProcessResumeUploaded') && !notesAdded.has(`${svc}:process`)) {
        const tokens = this.getTag(span, 'ai.tokens.total');
        notesAdded.add(`${svc}:process`);
        lines.push(`Note over ${alias}: 📄 Parse resume${tokens ? ` (${tokens} tokens)` : ''} (${dMs}ms)`);
      }

      if (op.includes('GenerateMatchExplanations') && !notesAdded.has(`${svc}:match`)) {
        const count = this.getTag(span, 'explanation.generated_count') ?? '?';
        notesAdded.add(`${svc}:match`);
        lines.push(`Note over ${alias}: 🎯 Generate ${count} match explanations (${dMs}ms)`);
      }

      if (op.includes('EmbedResume') && !notesAdded.has(`${svc}:embedresume`)) {
        notesAdded.add(`${svc}:embedresume`);
        lines.push(`Note over ${alias}: 📐 Embed resume vectors (${dMs}ms)`);
      }
    }

    // Compute end-to-end duration from earliest start to latest end
    const minStart = Math.min(...spans.map(s => s.startTime));
    const maxEnd = Math.max(...spans.map(s => s.startTime + s.duration));
    const durationMs = Math.round((maxEnd - minStart) / 1000);

    return {
      diagram: lines.join('\n'),
      serviceNames: [...usedServices],
      durationMs
    };
  }

  private getTag(span: any, key: string): string | undefined {
    return span.tags?.find((t: any) => t.key === key)?.value;
  }

  private async renderMermaid(diagram: string) {
    await new Promise(r => setTimeout(r, 50));
    if (!this.diagramContainer?.nativeElement) return;
    const container = this.diagramContainer.nativeElement;
    const id = `mermaid-${Date.now()}`;
    try {
      const { svg } = await mermaid.render(id, diagram);
      container.innerHTML = svg;
    } catch (e) {
      container.innerHTML = '<p class="text-color-secondary text-sm">Diagram rendering failed</p>';
    }
  }

  onClose() {
    this.fetchAttempt++; // cancel pending retries
    this.popup.close();
    this.event.set(null);
    this.services.set([]);
    this.traceDuration.set(null);
    this.diagramLoading.set(false);
    if (this.diagramContainer?.nativeElement) {
      this.diagramContainer.nativeElement.innerHTML = '';
    }
  }
}
