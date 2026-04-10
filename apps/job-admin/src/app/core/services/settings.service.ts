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

export interface ReEmbedJobsResponse {
  jobsProcessed: number;
}

export interface GenerateMatchExplanationsResponse {
  resumesProcessed: number;
  explanationsGenerated: number;
}

@Injectable({providedIn: 'root'})
export class SettingsService {
  private http = inject(HttpClient);

  private aiBase = `${environment.gatewayUrl}ai/v2/Settings`;

  getProvider() {
    return this.http.get<ApiResponse<ProviderSettings>>(
      `${this.aiBase}/provider`
    );
  }

  updateProvider(request: ProviderSettings) {
    return this.http.put<ApiResponse<UpdateProviderResponse>>(
      `${this.aiBase}/update-provider`,
      request
    );
  }

  getApplicationMode() {
    return this.http.get<ApiResponse<ApplicationMode>>(
      `${this.aiBase}/mode`
    );
  }

  updateApplicationMode(request: ApplicationMode) {
    return this.http.put<ApiResponse<ApplicationMode>>(
      `${this.aiBase}/mode`,
      request
    );
  }

  reEmbedAllJobs() {
    return this.http.post<ApiResponse<ReEmbedJobsResponse>>(
      `${this.aiBase}/re-embed-jobs`,
      {}
    );
  }

  generateMatchExplanations() {
    return this.http.post<ApiResponse<GenerateMatchExplanationsResponse>>(
      `${this.aiBase}/generate-match-explanations`,
      {}
    );
  }

  getFeatureFlags() {
    return this.http.get<ApiResponse<{ name: string; enabled: boolean }[]>>(
      `${this.aiBase}/feature-flags`
    );
  }

  updateFeatureFlag(request: { name: string; enabled: boolean }) {
    return this.http.put<ApiResponse<{ name: string; enabled: boolean }>>(
      `${this.aiBase}/feature-flags`,
      request
    );
  }
}
