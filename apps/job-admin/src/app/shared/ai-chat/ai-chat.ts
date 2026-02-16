import {Component, ElementRef, inject, signal, viewChild} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {ChatService} from '../../core/services/chat.service';

interface ChatMessage {
  role: 'user' | 'assistant';
  content: string;
  traceId?: string;
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
  conversationId = signal<string | undefined>(undefined);
  messages = signal<ChatMessage[]>([
    {role: 'assistant', content: 'Hello! I\'m your AI assistant. I can help you with job postings, companies, and more. How can I help you today?'}
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
      const show = command === 'show jaeger';
      this.showJaeger.set(show);
      localStorage.setItem(AiChat.JAEGER_KEY, String(show));
      this.message = '';
      this.focusInput();
      return;
    }

    this.messages.update(msgs => [...msgs, {role: 'user', content: text}]);
    this.message = '';
    this.loading.set(true);
    this.scrollToBottom();

    this.chatService.chat({
      message: text,
      conversationId: this.conversationId()
    }).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.conversationId.set(res.data.conversationId);
          this.messages.update(msgs => [...msgs, {role: 'assistant', content: res.data!.response, traceId: res.data!.traceId}]);
        } else {
          this.messages.update(msgs => [...msgs, {role: 'assistant', content: 'Sorry, something went wrong. Please try again.'}]);
        }
        this.loading.set(false);
        this.scrollToBottom();
        this.focusInput();
      },
      error: () => {
        this.messages.update(msgs => [...msgs, {role: 'assistant', content: 'Unable to reach the AI service. Please try again later.'}]);
        this.loading.set(false);
        this.scrollToBottom();
        this.focusInput();
      }
    });
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'q' && event.ctrlKey) {
      event.preventDefault();
      const show = !this.showJaeger();
      this.showJaeger.set(show);
      localStorage.setItem(AiChat.JAEGER_KEY, String(show));
      return;
    }
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
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
