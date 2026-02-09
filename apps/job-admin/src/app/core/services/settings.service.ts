import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {FeatureFlagsService} from './feature-flags.service';

export interface ProviderSettings {
  provider: string;
  model: string;
}

export interface UpdateProviderResponse {
  success: boolean;
}

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);
  private featureFlagsService = inject(FeatureFlagsService);

  private get baseUrl() {
    return this.featureFlagsService.isMonolith() ? environment.monolithUrl : environment.microserviceUrl;
  }

  getProvider() {
    return this.http.get<ApiResponse<ProviderSettings>>(
      `${this.baseUrl}settings/provider`
    );
  }

  updateProvider(request: ProviderSettings) {
    return this.http.put<ApiResponse<UpdateProviderResponse>>(
      `${this.baseUrl}settings/update-provider`,
      request
    );
  }
}
