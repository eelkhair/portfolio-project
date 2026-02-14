import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';

export interface ChatRequest {
  message: string;
  companyId?: string;
  conversationId?: string;
}

export interface ChatResponse {
  response: string;
  conversationId: string;
}

@Injectable({providedIn: 'root'})
export class ChatService {
  private http = inject(HttpClient);
  private baseUrl = environment.aiServiceUrl;

  chat(request: ChatRequest) {
    return this.http.post<ApiResponse<ChatResponse>>(
      `${this.baseUrl}chat`,
      request
    );
  }
}
