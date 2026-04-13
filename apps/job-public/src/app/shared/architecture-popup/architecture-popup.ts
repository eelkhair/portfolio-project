import { Component, effect, ElementRef, HostListener, inject, signal, ViewChild, ViewEncapsulation } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ArchitecturePopupService } from './architecture-popup.service';
import { ARCHITECTURE_EVENTS, ArchitectureEvent } from './diagrams';
import { DebugService } from '../../core/services/debug.service';
import { environment } from '../../../environments/environment';
import mermaid from 'mermaid';

@Component({
  selector: 'app-architecture-popup',
  encapsulation: ViewEncapsulation.None,
  template: `
    @if (popup.visible()) {
      <div class="fixed inset-0 z-50 flex items-start justify-center bg-black/40 backdrop-blur-sm animate-fade-in"
           (click)="onBackdropClick($event)">
        <div class="mx-4 mt-16 w-full max-w-[90vw] rounded-xl border border-slate-200 bg-white shadow-2xl dark:border-slate-700 dark:bg-slate-800 max-h-[85vh] overflow-y-auto"
             (click)="$event.stopPropagation()">

          <div class="flex items-center justify-between px-6 py-4 border-b border-slate-200 dark:border-slate-700">
            <div class="flex items-center gap-2">
              <svg class="h-5 w-5 text-primary-600 dark:text-primary-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M3.75 6A2.25 2.25 0 016 3.75h2.25A2.25 2.25 0 0110.5 6v2.25a2.25 2.25 0 01-2.25 2.25H6a2.25 2.25 0 01-2.25-2.25V6z" />
              </svg>
              <h2 class="text-lg font-bold text-slate-900 dark:text-white">What Just Happened?</h2>
            </div>
            <button (click)="popup.close()"
                    class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-400 hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-700 dark:hover:text-slate-200">
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2">
                <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          @if (event()) {
            <div class="px-6 py-4 flex flex-col gap-4">
              <div>
                <h3 class="text-xl font-bold text-slate-900 dark:text-white">{{ event()!.title }}</h3>
                <p class="text-sm text-slate-500 dark:text-slate-400 mt-1">{{ event()!.description }}</p>
              </div>

              @if (traceDuration()) {
                <div class="text-sm text-slate-500 dark:text-slate-400">
                  End-to-end: {{ traceDuration() }}ms
                </div>
              }

              <div class="flex flex-wrap gap-1">
                @for (svc of services(); track svc) {
                  <span class="text-xs px-2 py-1 rounded-full bg-primary-100 text-primary-700 dark:bg-primary-900/30 dark:text-primary-400 font-medium">{{ svc }}</span>
                }
              </div>

              <div class="overflow-auto rounded-lg bg-slate-50 dark:bg-slate-900 p-3 relative">
                @if (diagramLoading()) {
                  <div class="flex flex-col items-center justify-center py-12 gap-3">
                    <div class="h-10 w-10 animate-spin rounded-full border-4 border-primary-200 border-t-primary-600"></div>
                    <span class="text-sm text-slate-400">Building diagram from trace...</span>
                  </div>
                }
                @if (updating()) {
                  <div class="absolute top-2 right-2 flex items-center gap-1.5 px-2 py-1 rounded-full bg-amber-100 dark:bg-amber-900/30">
                    <div class="h-3 w-3 animate-spin rounded-full border-2 border-amber-300 border-t-amber-600"></div>
                    <span class="text-xs text-amber-600 dark:text-amber-400">Updating...</span>
                  </div>
                }
                <div #diagramContainer></div>
              </div>

              @if (popup.traceId()) {
                <div class="flex gap-4">
                  <a class="text-sm font-semibold text-amber-600 dark:text-amber-400 hover:underline"
                     [href]="jaegerUrl + popup.traceId()"
                     target="_blank" rel="noopener">
                    View in Jaeger →
                  </a>
                  <a class="text-sm font-semibold text-orange-600 dark:text-orange-400 hover:underline"
                     [href]="grafanaUrl + popup.traceId()"
                     target="_blank" rel="noopener">
                    View in Grafana →
                  </a>
                </div>
              }
            </div>
          }
        </div>
      </div>
    }
  `,
})
export class ArchitecturePopup {
  readonly popup = inject(ArchitecturePopupService);
  private readonly debug = inject(DebugService);
  private readonly http = inject(HttpClient);

  readonly jaegerUrl = (environment as any).jaegerUrl ?? '';
  readonly grafanaUrl = (environment as any).grafanaUrl ?? '';
  // Fetch traces through the gateway's /jaeger-api/ proxy route
  private readonly jaegerApiUrl = ((environment as any).apiUrl ?? '').replace('/api/', '/') + 'jaeger-api/api/traces/';

  @ViewChild('diagramContainer') diagramContainer?: ElementRef;

  readonly event = signal<ArchitectureEvent | null>(null);
  readonly services = signal<string[]>([]);
  readonly diagramLoading = signal(false);
  readonly updating = signal(false);
  readonly traceDuration = signal<number | null>(null);

  private fetchAttempt = 0;

  constructor() {
    mermaid.initialize({ startOnLoad: false, theme: 'dark', themeVariables: { fontSize: '12px' } });

    effect(() => {
      const type = this.popup.eventType();
      if (!type) return;

      const resolved = ARCHITECTURE_EVENTS[type];
      if (resolved) this.event.set(resolved);
      this.diagramLoading.set(true);

      if (!this.popup.traceId()) {
        const last = this.debug.entries()[0];
        if (last) {
          this.popup.traceId.set(last.traceId);
          this.popup.duration.set(last.duration);
        }
      }

      const traceId = this.popup.traceId();
      if (traceId) {
        // Resume parsing takes ~19s end-to-end; fetch early, retry for late spans
        const delay = type === 'resume-parse' ? 8000 : 4000;
        setTimeout(() => this.fetchAndRenderTrace(traceId), delay);
      }
    });
  }

  private fetchAndRenderTrace(traceId: string) {
    this.diagramLoading.set(true);
    this.fetchAttempt++;
    const attempt = this.fetchAttempt;

    this.http.get<any>(`${this.jaegerApiUrl}${traceId}`).subscribe({
      next: (response) => {
        if (attempt !== this.fetchAttempt) return;
        const trace = response?.data?.[0];
        if (trace) {
          const { diagram, serviceNames, durationMs } = this.buildDiagramFromTrace(trace);
          this.services.set(serviceNames);
          this.traceDuration.set(durationMs);
          this.renderMermaid(diagram);

          // Retry to pick up late-arriving spans (SignalR, match explanations)
          const retryDelays = [8000, 16000];
          let lastSpanCount = trace.spans.length;
          for (const delay of retryDelays) {
            setTimeout(() => {
              if (attempt !== this.fetchAttempt) return;
              this.updating.set(true);
              this.http.get<any>(`${this.jaegerApiUrl}${traceId}`).subscribe({
                next: (retry) => {
                  this.updating.set(false);
                  const retryTrace = retry?.data?.[0];
                  if (retryTrace && retryTrace.spans.length > lastSpanCount) {
                    lastSpanCount = retryTrace.spans.length;
                    const result = this.buildDiagramFromTrace(retryTrace);
                    this.services.set(result.serviceNames);
                    this.traceDuration.set(result.durationMs);
                    this.renderMermaid(result.diagram);
                  }
                },
                error: () => this.updating.set(false)
              });
            }, delay);
          }
        }
        this.diagramLoading.set(false);
      },
      error: () => this.diagramLoading.set(false)
    });
  }

  private buildDiagramFromTrace(trace: any): { diagram: string; serviceNames: string[]; durationMs: number } {
    const processes = trace.processes as Record<string, { serviceName: string }>;
    const spans = (trace.spans as any[]).sort((a, b) => a.startTime - b.startTime);
    const spanMap = new Map(spans.map(s => [s.spanID, s]));

    const resolveService = (pid: string): string => {
      const name = processes[pid]?.serviceName ?? 'unknown';
      return name.replace(/-dapr-cli$/, '').replace(/-dapr$/, '');
    };

    const serviceOrder: string[] = [];
    const serviceSet = new Set<string>();
    for (const span of spans) {
      const svc = resolveService(span.processID);
      if (!serviceSet.has(svc)) { serviceSet.add(svc); serviceOrder.push(svc); }
    }

    const isNoise = (span: any): boolean => {
      const op = span.operationName;
      const kind = this.getTag(span, 'span.kind');
      if (this.getTag(span, 'db.system')) return true;
      if (op.startsWith('/dapr.proto')) return true;
      if (kind === 'client' && /^(GET|POST|PUT|DELETE|PATCH)$/.test(op)) return true;
      return false;
    };

    const interestingSpans = spans.filter(s => !isNoise(s));

    const topicProducers = new Map<string, string>();
    for (const span of interestingSpans) {
      const topic = this.getTag(span, 'messaging.destination.name');
      const kind = this.getTag(span, 'span.kind');
      if (topic && (span.operationName.startsWith('pubsub/') || kind === 'producer')) {
        topicProducers.set(topic, resolveService(span.processID));
      }
    }

    const orphanRoots = new Map<string, any>();
    for (const span of interestingSpans) {
      const parentRef = span.references?.find((r: any) => r.refType === 'CHILD_OF');
      if (parentRef && !spanMap.has(parentRef.spanID)) {
        orphanRoots.set(span.spanID, span);
      }
    }

    const lines = ['sequenceDiagram'];
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

      if (!parentRef) {
        const serverChild = interestingSpans.find(s => {
          const ref = s.references?.find((r: any) => r.refType === 'CHILD_OF');
          return ref?.spanID === span.spanID && resolveService(s.processID) !== childSvc;
        });
        if (serverChild) addArrow(childSvc, resolveService(serverChild.processID), label, durationMs, false);
        continue;
      }

      if (spanMap.has(parentRef.spanID)) {
        const parentSpan = spanMap.get(parentRef.spanID)!;
        const parentSvc = resolveService(parentSpan.processID);
        const kind = this.getTag(span, 'span.kind');
        const isAsync = kind === 'consumer' || label.includes('📨') || label.includes('signalr');
        addArrow(parentSvc, childSvc, label, durationMs, isAsync);
        continue;
      }

      if (orphanRoots.has(span.spanID)) {
        const topic = this.getTag(span, 'messaging.destination.name');
        if (topic) {
          const producerSvc = topicProducers.get(topic);
          if (producerSvc) { addArrow(producerSvc, childSvc, `📨 ${topic}`, durationMs, true); continue; }
        }
        const pubsubParent = interestingSpans.find(s => {
          if (!s.operationName.startsWith('pubsub/')) return false;
          return resolveService(s.processID) === childSvc && s.startTime <= span.startTime && (s.startTime + s.duration) >= span.startTime;
        });
        if (pubsubParent) continue;
        const closestPubsub = [...interestingSpans]
          .filter(s => s.operationName.startsWith('pubsub/') && s.startTime <= span.startTime)
          .sort((a, b) => b.startTime - a.startTime)[0];
        if (closestPubsub) {
          const producerSvc = resolveService(closestPubsub.processID);
          addArrow(producerSvc, childSvc, label, durationMs, true);
        }
      }
    }

    // Add notes for key internal operations (OpenAI calls, embeddings, etc.)
    const notesAdded = new Set<string>();
    for (const span of spans) {
      const svc = resolveService(span.processID);
      if (!usedServices.has(svc)) continue;
      const alias = svc.replace(/[^a-zA-Z0-9]/g, '_');
      const op = span.operationName;
      const dMs = Math.round(span.duration / 1000);

      // OpenAI API calls
      const urlTag = this.getTag(span, 'url.full') ?? '';
      if (urlTag.includes('api.openai.com') && !notesAdded.has(`${svc}:openai:${op}`)) {
        const endpoint = urlTag.includes('chat/completions') ? 'LLM completion' :
                         urlTag.includes('embeddings') ? 'Embedding generation' : 'OpenAI call';
        notesAdded.add(`${svc}:openai:${op}`);
        lines.push(`Note over ${alias}: 🤖 ${endpoint} (${dMs}ms)`);
      }

      // Embedding batch operations
      if (op === 'text.embedding.batch' && !notesAdded.has(`${svc}:embed`)) {
        const count = this.getTag(span, 'embedding.batch.count') ?? '?';
        notesAdded.add(`${svc}:embed`);
        lines.push(`Note over ${alias}: 📐 ${count}x embeddings (${dMs}ms)`);
      }

      // Resume processing handler
      if (op.includes('ProcessResumeUploaded') && !notesAdded.has(`${svc}:process`)) {
        const tokens = this.getTag(span, 'ai.tokens.total');
        notesAdded.add(`${svc}:process`);
        lines.push(`Note over ${alias}: 📄 Parse resume${tokens ? ` (${tokens} tokens)` : ''} (${dMs}ms)`);
      }

      // Match explanations
      if (op.includes('GenerateMatchExplanations') && !notesAdded.has(`${svc}:match`)) {
        const count = this.getTag(span, 'explanation.generated_count') ?? '?';
        notesAdded.add(`${svc}:match`);
        lines.push(`Note over ${alias}: 🎯 Generate ${count} match explanations (${dMs}ms)`);
      }

      // Embed resume
      if (op.includes('EmbedResume') && !notesAdded.has(`${svc}:embedresume`)) {
        notesAdded.add(`${svc}:embedresume`);
        lines.push(`Note over ${alias}: 📐 Embed resume vectors (${dMs}ms)`);
      }

      // SignalR sends
      if (op === 'signalr.message.send') {
        const dest = this.getTag(span, 'messaging.destination.name') ?? '';
        if (dest && !notesAdded.has(`${svc}:signalr:${dest}`)) {
          notesAdded.add(`${svc}:signalr:${dest}`);
        }
      }
    }

    const minStart = Math.min(...spans.map(s => s.startTime));
    const maxEnd = Math.max(...spans.map(s => s.startTime + s.duration));
    const totalDurationMs = Math.round((maxEnd - minStart) / 1000);

    return { diagram: lines.join('\n'), serviceNames: [...usedServices], durationMs: totalDurationMs };
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
    } catch {
      container.innerHTML = '<p class="text-sm text-slate-400">Diagram rendering failed</p>';
    }
  }

  onBackdropClick(event: MouseEvent) {
    if (event.target === event.currentTarget) this.closeAndReset();
  }

  @HostListener('document:keydown.escape')
  onEscape() { if (this.popup.visible()) this.closeAndReset(); }

  private closeAndReset() {
    this.fetchAttempt++; // cancel pending retries
    this.popup.close();
    this.event.set(null);
    this.services.set([]);
    this.traceDuration.set(null);
    this.diagramLoading.set(false);
    this.updating.set(false);
    if (this.diagramContainer?.nativeElement) {
      this.diagramContainer.nativeElement.innerHTML = '';
    }
  }
}
