import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';

export interface UpdateProviderRequest {
  provider: string;
  model: string;
}

export interface UpdateProviderResponse {
  success: boolean;
}

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);

  updateProvider(request: UpdateProviderRequest) {
    return this.http.put<ApiResponse<UpdateProviderResponse>>(
      `${environment.apiUrl}settings/update-provider`,
      request
    );
  }
}
