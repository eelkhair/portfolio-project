import {Component, ElementRef, inject, signal, viewChild} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {ChatService, ChatStreamDone} from '../../core/services/chat.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
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
  private abortController: AbortController | null = null;

  isOpen = signal(false);
  message = '';
  loading = signal(false);
  conversationId = signal<string | undefined>(undefined);
  messages = signal<ChatMessage[]>([
    {role: 'assistant', content: 'Hello! I\'m your AI assistant. I can help you with job postings, companies, and more. How can I help you today?'}
  ]);

  chatInput = viewChild<ElementRef<HTMLTextAreaElement>>('chatInput');
  messagesContainer = viewChild<ElementRef<HTMLDivElement>>('messagesContainer');

  toggle() {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      setTimeout(() => this.chatInput()?.nativeElement.focus(), 0);
    }
  }

  async send() {
    const text = this.message.trim();
    if (!text || this.loading()) return;

    this.messages.update(msgs => [...msgs, {role: 'user', content: text}]);
    this.message = '';
    this.loading.set(true);
    this.scrollToBottom();

    this.messages.update(msgs => [...msgs, {role: 'assistant', content: ''}]);

    this.abortController = new AbortController();

    try {
      const stream = this.chatService.chatStream(
        {message: text, conversationId: this.conversationId()},
        this.abortController.signal
      );

      for await (const chunk of stream) {
        if (typeof chunk === 'string') {
          this.messages.update(msgs => {
            const updated = [...msgs];
            const last = updated[updated.length - 1];
            updated[updated.length - 1] = {...last, content: last.content + chunk};
            return updated;
          });
          this.scrollToBottom();
        } else {
          const done = chunk as ChatStreamDone;
          this.conversationId.set(done.conversationId);
        }
      }
    } catch (err: any) {
      if (err.name === 'AbortError') return;
      this.messages.update(msgs => {
        const updated = [...msgs];
        const last = updated[updated.length - 1];
        if (!last.content) {
          updated[updated.length - 1] = {...last, content: 'Unable to reach the AI service. Please try again later.'};
        }
        return updated;
      });
    } finally {
      this.loading.set(false);
      this.abortController = null;
      this.scrollToBottom();
    }
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }

  private scrollToBottom() {
    setTimeout(() => {
      const el = this.messagesContainer()?.nativeElement;
      if (el) el.scrollTop = el.scrollHeight;
    }, 0);
  }
}
