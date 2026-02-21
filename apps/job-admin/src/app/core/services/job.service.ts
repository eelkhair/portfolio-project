import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {Job} from '../types/models/Job';
import {JobGenRequest, JobGenResponse} from '../types/Dtos/JobGen';
import {Draft} from '../types/Dtos/draft';
import {EnhancementRequest, EnhancementResponse} from '../types/Dtos/EnhancementDto';
import {CreateJobDto} from '../types/Dtos/CreateJobRequest';

@Injectable({ providedIn: 'root' })
export class JobService {
  private http: HttpClient = inject(HttpClient);

  list(companyUId: string){
    return this.http.get<ApiResponse<Job[]>>(`${environment.gatewayUrl}jobs/${companyUId}`);
  }

  generateDraft(uId: string, payload: JobGenRequest) {
    return this.http.post<ApiResponse<JobGenResponse>>(`${environment.gatewayUrl}jobs/${uId}/generate`, payload);
  }

  saveDraft(uId: string, payload: Draft) {
    return this.http.put<ApiResponse<Draft>>(`${environment.gatewayUrl}jobs/${uId}/save-draft`, payload);
  }

  loadDrafts(companyId: string) {
    return this.http.get<ApiResponse<Draft[]>>(`${environment.gatewayUrl}jobs/${companyId}/list-drafts`);
  }

  rewrite(model: EnhancementRequest) {
    return this.http.put<ApiResponse<EnhancementResponse>>(`${environment.gatewayUrl}jobs/drafts/rewrite`, model);
  }

  createJob(model: CreateJobDto) {
    return this.http.post<ApiResponse<Job[]>>(`${environment.gatewayUrl}jobs`, model);
  }
}
