import {inject, Injectable, signal} from '@angular/core';
import {ChatService, ToolData} from '../../core/services/chat.service';
import {marked} from 'marked';
import {Observable, tap} from 'rxjs';
import {environment} from '../../../environments/environment';

export interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  html?: string;
  traceId?: string;
  duration?: string;
  time: string;
  toolData?: ToolData[];
}

@Injectable({providedIn: 'root'})
export class ChatStore {
  private readonly chatService = inject(ChatService);
  private static readonly JAEGER_KEY = 'ai-chat-show-jaeger';

  private readonly welcomeMessage = 'Hello! I\'m your AI assistant. I can help you with job postings, companies, and more. How can I help you today?';

  readonly messages = signal<ChatMessage[]>([
    {role: 'assistant', content: this.welcomeMessage, html: marked.parse(this.welcomeMessage, {async: false}) as string, time: this.formatTime()}
  ]);
  readonly loading = signal(false);
  readonly conversationId = signal<string | undefined>(undefined);
  readonly showJaeger = signal(localStorage.getItem(ChatStore.JAEGER_KEY) === 'true');
  readonly jaegerNotification = signal<string | null>(null);
  readonly grafanaUrl = signal(environment.grafanaUrl);
  readonly jaegerUrl = signal(environment.jaegerUrl);

  send(text: string): Observable<void> {
    this.messages.update(msgs => [...msgs, {role: 'user', content: text, time: this.formatTime()}]);
    this.loading.set(true);

    const startTime = performance.now();

    return this.chatService.chat({
      message: text,
      conversationId: this.conversationId()
    }).pipe(
      tap({
        next: (res) => {
          const duration = this.formatDuration(performance.now() - startTime);
          if (res.success && res.data) {
            this.conversationId.set(res.data.conversationId);

            if (res.data.data?.length) {
              // Direct return — structured tool data, render as markdown table
              const content = this.renderToolData(res.data.data);
              this.messages.update(msgs => [...msgs, {
                role: 'assistant', content, html: this.renderMarkdown(content),
                traceId: res.data!.traceId, duration, time: this.formatTime(),
                toolData: res.data!.data
              }]);
            } else {
              // Normal LLM text response
              const content = res.data.response;
              this.messages.update(msgs => [...msgs, {
                role: 'assistant', content, html: this.renderMarkdown(content),
                traceId: res.data!.traceId, duration, time: this.formatTime()
              }]);
            }
          } else {
            const content = 'Sorry, something went wrong. Please try again.';
            this.messages.update(msgs => [...msgs, {role: 'assistant', content, html: this.renderMarkdown(content), duration, time: this.formatTime()}]);
          }
          this.loading.set(false);
        },
        error: () => {
          const duration = this.formatDuration(performance.now() - startTime);
          const content = 'Unable to reach the AI service. Please try again later.';
          this.messages.update(msgs => [...msgs, {role: 'assistant', content, html: this.renderMarkdown(content), duration, time: this.formatTime()}]);
          this.loading.set(false);
        }
      })
    ) as unknown as Observable<void>;
  }

  toggleJaeger(show: boolean): void {
    this.showJaeger.set(show);
    localStorage.setItem(ChatStore.JAEGER_KEY, String(show));
    this.jaegerNotification.set(show ? 'debugging mode on' : 'debugging mode off');
    setTimeout(() => this.jaegerNotification.set(null), 2000);
  }

  private formatTime(): string {
    return new Date().toLocaleTimeString([], {hour: '2-digit', minute: '2-digit'});
  }

  private formatDuration(ms: number): string {
    const totalMs = Math.round(ms);
    const totalSec = Math.floor(totalMs / 1000);
    const remainMs = totalMs % 1000;
    if (totalSec < 60) return `${totalSec}s ${remainMs}ms`;
    const min = Math.floor(totalSec / 60);
    const sec = totalSec % 60;
    return sec > 0 ? `${min}min ${sec}s ${remainMs}ms` : `${min}min ${remainMs}ms`;
  }

  private static readonly HIDDEN_KEYS = new Set(['id', 'uid', 'companyid', 'companyuid', 'industryuid']);

  private renderToolData(data: ToolData[]): string {
    return data.map(d => {
      const rows = Array.isArray(d.result) ? d.result : [d.result];
      if (rows.length === 0) return 'No results found.';

      // Find a display name field
      const nameKey = Object.keys(rows[0] ?? {}).find(k =>
        ['name', 'title', 'companyname'].includes(k.toLowerCase())
      );

      if (nameKey && rows.length > 1) {
        // Simple numbered list of names
        const list = rows.map((row: Record<string, unknown>, i: number) =>
          `${i + 1}. ${row[nameKey] ?? '-'}`
        ).join('\n');
        return `**${rows.length} results**\n\n${list}`;
      }

      // Single result or no name field — show key/value pairs
      const allKeys = Object.keys(rows[0] ?? {});
      const keys = allKeys.filter(k => !ChatStore.HIDDEN_KEYS.has(k.toLowerCase()));
      if (keys.length === 0) return `${JSON.stringify(d.result)}`;

      return rows.map((row: Record<string, unknown>) =>
        keys.map(k => `- **${this.humanize(k)}:** ${this.formatCell(row[k])}`).join('\n')
      ).join('\n\n');
    }).join('\n\n');
  }

  private humanize(key: string): string {
    return key.replace(/([A-Z])/g, ' $1').replace(/^./, s => s.toUpperCase()).trim();
  }

  private formatCell(value: unknown): string {
    if (value == null) return '-';
    if (Array.isArray(value)) return `${value.length} items`;
    return String(value);
  }

  private renderMarkdown(content: string): string {
    return marked.parse(content, {async: false}) as string;
  }
}
