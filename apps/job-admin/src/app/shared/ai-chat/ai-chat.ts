import {Component, ElementRef, inject, signal, viewChild} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {ChatService} from '../../core/services/chat.service';
import {marked} from 'marked';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  html?: string;
  traceId?: string;
  duration?: string;
  time: string;
}

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [FormsModule, Button, InputText],
  templateUrl: './ai-chat.html',
  styleUrl: './ai-chat.css'
})
export class AiChat {
  private chatService = inject(ChatService);
  private static readonly JAEGER_KEY = 'ai-chat-show-jaeger';

  isOpen = signal(false);
  message = '';
  loading = signal(false);
  showJaeger = signal(localStorage.getItem(AiChat.JAEGER_KEY) === 'true');
  jaegerNotification = signal<string | null>(null);
  conversationId = signal<string | undefined>(undefined);
  private readonly welcomeMessage = 'Hello! I\'m your AI assistant. I can help you with job postings, companies, and more. How can I help you today?';
  messages = signal<ChatMessage[]>([
    {role: 'assistant', content: this.welcomeMessage, html: marked.parse(this.welcomeMessage, {async: false}) as string, time: this.formatTime()}
  ]);

  chatInput = viewChild<ElementRef<HTMLTextAreaElement>>('chatInput');
  messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  toggle() {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      this.focusInput();
    }
  }

  send() {
    const text = this.message.trim();
    if (!text || this.loading()) return;

    const command = text.toLowerCase();
    if (command === 'show jaeger' || command === 'hide jaeger') {
      this.toggleJaeger(command === 'show jaeger');
      this.message = '';
      this.focusInput();
      return;
    }

    this.messages.update(msgs => [...msgs, {role: 'user', content: text, time: this.formatTime()}]);
    this.message = '';
    this.loading.set(true);
    this.scrollToBottom();

    const startTime = performance.now();

    this.chatService.chat({
      message: text,
      conversationId: this.conversationId()
    }).subscribe({
      next: (res) => {
        const duration = this.formatDuration(performance.now() - startTime);
        if (res.success && res.data) {
          this.conversationId.set(res.data.conversationId);
          const content = res.data!.response;
          this.messages.update(msgs => [...msgs, {role: 'assistant', content, html: this.renderMarkdown(content), traceId: res.data!.traceId, duration, time: this.formatTime()}]);
        } else {
          const content = 'Sorry, something went wrong. Please try again.';
          this.messages.update(msgs => [...msgs, {role: 'assistant', content, html: this.renderMarkdown(content), duration, time: this.formatTime()}]);
        }
        this.loading.set(false);
        this.scrollToBottom();
        this.focusInput();
      },
      error: () => {
        const duration = this.formatDuration(performance.now() - startTime);
        const content = 'Unable to reach the AI service. Please try again later.';
        this.messages.update(msgs => [...msgs, {role: 'assistant', content, html: this.renderMarkdown(content), duration, time: this.formatTime()}]);
        this.loading.set(false);
        this.scrollToBottom();
        this.focusInput();
      }
    });
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'q' && event.ctrlKey) {
      event.preventDefault();
      this.toggleJaeger(!this.showJaeger());
      return;
    }
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  private toggleJaeger(show: boolean) {
    this.showJaeger.set(show);
    localStorage.setItem(AiChat.JAEGER_KEY, String(show));
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

  private renderMarkdown(content: string): string {
    return marked.parse(content, {async: false}) as string;
  }

  private focusInput() {
    setTimeout(() => this.chatInput()?.nativeElement.focus(), 0);
  }

  private scrollToBottom() {
    setTimeout(() => {
      const el = this.messagesContainer()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
