import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {AuthService} from '@auth0/auth0-angular';
import {firstValueFrom} from 'rxjs';

export interface ChatRequest {
  message: string;
  companyId?: string;
  conversationId?: string;
}

export interface ChatResponse {
  response: string;
  conversationId: string;
}

export interface ChatStreamDone {
  done: true;
  conversationId: string;
  traceId: string;
}

@Injectable({providedIn: 'root'})
export class ChatService {
  private http = inject(HttpClient);
  private auth = inject(AuthService);
  private baseUrl = environment.aiServiceUrl;

  chat(request: ChatRequest) {
    return this.http.post<ApiResponse<ChatResponse>>(
      `${this.baseUrl}chat`,
      request
    );
  }

  async *chatStream(request: ChatRequest, signal?: AbortSignal): AsyncGenerator<string | ChatStreamDone> {
    const token = await firstValueFrom(this.auth.getAccessTokenSilently());

    const response = await fetch(`${this.baseUrl}chat/stream`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(request),
      signal
    });

    if (!response.ok) {
      throw new Error(`Stream request failed: ${response.status}`);
    }

    const reader = response.body!.getReader();
    const decoder = new TextDecoder();
    let buffer = '';

    while (true) {
      const {done, value} = await reader.read();
      if (done) break;

      buffer += decoder.decode(value, {stream: true});
      const lines = buffer.split('\n');
      buffer = lines.pop()!;

      for (const line of lines) {
        if (!line.startsWith('data: ')) continue;
        const json = line.slice(6);
        const parsed = JSON.parse(json);

        if (parsed.done) {
          yield parsed as ChatStreamDone;
        } else {
          yield parsed.content as string;
        }
      }
    }
  }
}
