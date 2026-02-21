import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {map} from 'rxjs';

export interface ChatRequest {
  message: string;
  companyId?: string;
  conversationId?: string;
}

export interface ChatResponse {
  response: string;
  conversationId: string;
  traceId?: string;
}

@Injectable({providedIn: 'root'})
export class ChatService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.gatewayUrl}ai/v2/`;

  chat(request: ChatRequest) {
    return this.http.post<ApiResponse<ChatResponse>>(
      `${this.baseUrl}chat`,
      request,
      {observe: 'response'}
    ).pipe(
      map(res => {
        const body = res.body!;
        if (body.data) {
          body.data.traceId = res.headers.get('x-trace-id') ?? undefined;
        }
        return body;
      })
    );
  }
}
