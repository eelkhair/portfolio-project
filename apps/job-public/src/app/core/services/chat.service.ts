import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map } from 'rxjs';
import { ApiResponse } from '../types/api-response.type';
import { environment } from '../../../environments/environment';

export interface ChatRequest {
  message: string;
  conversationId?: string;
}

export interface ChatResponse {
  response: string;
  conversationId: string;
  traceId?: string;
}

@Injectable({ providedIn: 'root' })
export class ChatService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.aiUrl;

  chat(request: ChatRequest) {
    return this.http
      .post<ApiResponse<ChatResponse>>(`${this.baseUrl}chat/public`, request, {
        observe: 'response',
      })
      .pipe(
        map((res) => {
          const body = res.body!;
          if (body.data) {
            body.data.traceId = res.headers.get('x-trace-id') ?? undefined;
          }
          return body;
        }),
      );
  }
}
