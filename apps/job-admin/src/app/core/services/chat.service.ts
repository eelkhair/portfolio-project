import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {map} from 'rxjs';
import {AccountService} from './account.service';
import {ActivityLogger} from './activity-logger.service';

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
  private logger = inject(ActivityLogger);
  private baseUrl = `${environment.gatewayUrl}ai/v2/`;

  private get chatEndpoint(): string {
    const isSystemAdmin = this.accountService.groups()
      .some(g => g.replace(/^\//, '') === 'SystemAdmins');
    return isSystemAdmin ? 'chat/system' : 'chat';
  }

  chat(request: ChatRequest) {
    const endpoint = this.chatEndpoint;
    return this.http.post<ApiResponse<ChatResponse>>(
      `${this.baseUrl}${endpoint}`,
      request,
      {observe: 'response'}
    ).pipe(
      map(res => {
        const body = res.body!;
        if (body.data) {
          body.data.traceId = res.headers.get('x-trace-id') ?? undefined;
        }
        return body;
      }),
      this.logger.trace('chat send', (body) => ({
        endpoint,
        conversationId: body.data?.conversationId,
        toolCount: body.data?.toolResults?.length ?? 0,
        messageLength: request.message.length,
      })),
    );
  }
}
