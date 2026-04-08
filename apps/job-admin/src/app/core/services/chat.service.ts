import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {map} from 'rxjs';
import {AccountService} from './account.service';

export interface ChatRequest {
  message: string;
  companyId?: string;
  conversationId?: string;
}

export interface ToolData {
  tool: string;
  result: unknown;
}

export interface ChatResponse {
  response: string;
  conversationId: string;
  traceId?: string;
  toolResults?: ToolData[];
}

@Injectable({providedIn: 'root'})
export class ChatService {
  private http = inject(HttpClient);
  private accountService = inject(AccountService);
  private baseUrl = `${environment.gatewayUrl}ai/v2/`;

  private get chatEndpoint(): string {
    const isSystemAdmin = this.accountService.groups()
      .some(g => g.replace(/^\//, '') === 'SystemAdmins');
    return isSystemAdmin ? 'chat/system' : 'chat';
  }

  chat(request: ChatRequest) {
    return this.http.post<ApiResponse<ChatResponse>>(
      `${this.baseUrl}${this.chatEndpoint}`,
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
