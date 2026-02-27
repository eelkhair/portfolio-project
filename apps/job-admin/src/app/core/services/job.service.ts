import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {Job} from '../types/models/Job';
import {JobGenRequest, JobGenResponse} from '../types/Dtos/JobGen';
import {Draft} from '../types/Dtos/draft';
import {EnhancementRequest, EnhancementResponse} from '../types/Dtos/EnhancementDto';
import {CreateJobDto} from '../types/Dtos/CreateJobRequest';
import {FeatureFlagsService} from './feature-flags.service';
import {map} from 'rxjs';

@Injectable({ providedIn: 'root' })
export class JobService {
  private http: HttpClient = inject(HttpClient);
  private featureFlags = inject(FeatureFlagsService);

  private get baseUrl(): string {
    return environment.gatewayUrl;
  }

  list(companyUId: string) {
    return this.http.get<any>(`${this.baseUrl}jobs/${companyUId}`).pipe(
      map(res => this.normalize(res))
    );
  }

  generateDraft(uId: string, payload: JobGenRequest) {
    return this.http.post<ApiResponse<JobGenResponse>>(`${this.baseUrl}jobs/${uId}/generate`, payload);
  }

  saveDraft(uId: string, payload: Draft) {
    return this.http.put<ApiResponse<Draft>>(`${this.baseUrl}jobs/${uId}/save-draft`, payload);
  }

  loadDrafts(companyId: string) {
    return this.http.get<ApiResponse<Draft[]>>(`${this.baseUrl}jobs/${companyId}/list-drafts`);
  }

  rewrite(model: EnhancementRequest) {
    return this.http.put<ApiResponse<EnhancementResponse>>(`${this.baseUrl}jobs/drafts/rewrite`, model);
  }

  createJob(model: CreateJobDto) {
    return this.http.post<ApiResponse<Job[]>>(`${this.baseUrl}jobs`, model);
  }

  private normalize(response: any): ApiResponse<Job[]> {
    // ApiResponse format (admin-api / gateway)
    if (response?.data) return response;
    // Plain array (monolith) or OData envelope
    const items: any[] = Array.isArray(response) ? response : response?.value ?? [];
    return {
      data: items.map(item => ({...item, uId: item.uId ?? item.id})),
      success: true,
      statusCode: 200
    };
  }
}
