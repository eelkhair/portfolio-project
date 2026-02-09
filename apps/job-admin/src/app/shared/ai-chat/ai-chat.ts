import { Component, ElementRef, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputText } from 'primeng/inputtext';

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
  isOpen = signal(false);
  message = '';
  messages = signal<ChatMessage[]>([
    { role: 'assistant', content: 'Hello! I\'m your AI assistant. I can help you with job postings, candidate searches, and more. How can I help you today?' },
    { role: 'user', content: 'Can you help me write a job description for a Senior Software Engineer?' },
    { role: 'assistant', content: 'I\'d be happy to help you create a compelling job description for a Senior Software Engineer position. To get started, could you tell me:\n\n1. What technologies/stack will they work with?\n2. Is this remote, hybrid, or on-site?\n3. What team will they be joining?' },
    { role: 'user', content: 'It\'s a full-stack role using .NET and Angular, hybrid in Seattle' },
    { role: 'assistant', content: 'Great! Here\'s a draft for your Senior Software Engineer role:\n\n**About the Role**\nWe\'re looking for a Senior Software Engineer to join our Seattle team in a hybrid capacity. You\'ll build and maintain full-stack applications using .NET and Angular.\n\nWould you like me to expand on any section or adjust the tone?' }
  ]);

  chatInput = viewChild<ElementRef<HTMLInputElement>>('chatInput');

  toggle() {
    this.isOpen.update(v => !v);
    if (this.isOpen()) {
      setTimeout(() => this.chatInput()?.nativeElement.focus(), 0);
    }
  }

  send() {
    if (!this.message.trim()) return;

    this.messages.update(msgs => [
      ...msgs,
      { role: 'user', content: this.message }
    ]);

    this.message = '';

    // Simulate AI response
    setTimeout(() => {
      this.messages.update(msgs => [
        ...msgs,
        { role: 'assistant', content: 'This is a demo response. Backend not connected.' }
      ]);
    }, 500);
  }

  onKeydown(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.send();
    }
  }
}
