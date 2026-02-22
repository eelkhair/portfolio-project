import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';

export interface ProviderSettings {
  provider: string;
  model: string;
}

export interface UpdateProviderResponse {
  success: boolean;
}

export interface ApplicationMode {
  isMonolith: boolean;
}

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);

  getProvider() {
    return this.http.get<ApiResponse<ProviderSettings>>(
      `${environment.gatewayUrl}settings/provider`
    );
  }

  updateProvider(request: ProviderSettings) {
    return this.http.put<ApiResponse<UpdateProviderResponse>>(
      `${environment.gatewayUrl}settings/update-provider`,
      request
    );
  }

  getApplicationMode() {
    return this.http.get<ApiResponse<ApplicationMode>>(
      `${environment.gatewayUrl}settings/mode`
    );
  }

  updateApplicationMode(request: ApplicationMode) {
    return this.http.put<ApiResponse<ApplicationMode>>(
      `${environment.gatewayUrl}settings/mode`,
      request
    );
  }
}
