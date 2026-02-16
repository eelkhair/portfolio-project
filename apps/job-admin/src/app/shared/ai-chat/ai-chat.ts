import {Component, ElementRef, inject, signal, viewChild} from '@angular/core';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {ChatStore} from './ai-chat.store';

@Component({
  selector: 'app-ai-chat',
  standalone: true,
  imports: [FormsModule, Button, InputText],
  templateUrl: './ai-chat.html',
  styleUrl: './ai-chat.css'
})
export class AiChat {
  readonly store = inject(ChatStore);

  isOpen = signal(false);
  message = '';

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
    if (!text || this.store.loading()) return;

    const command = text.toLowerCase();
    if (command === 'show jaeger' || command === 'hide jaeger') {
      this.store.toggleJaeger(command === 'show jaeger');
      this.message = '';
      this.focusInput();
      return;
    }

    this.message = '';
    this.scrollToBottom();

    this.store.send(text).subscribe({
      next: () => {
        this.scrollToBottom();
        this.focusInput();
      },
      error: () => {
        this.scrollToBottom();
        this.focusInput();
      }
    });
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'q' && event.ctrlKey) {
      event.preventDefault();
      this.store.toggleJaeger(!this.store.showJaeger());
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
