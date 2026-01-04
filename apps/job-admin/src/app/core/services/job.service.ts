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

@Injectable({ providedIn: 'root' })
export class JobService {
  private http: HttpClient = inject(HttpClient);
  private featureFlagService = inject(FeatureFlagsService);

  list(companyUId: string){
    return this.http.get<ApiResponse<Job[]>>(environment.apiUrl+ 'jobs/'+companyUId);
  }

  generateDraft(uId: string, payload: JobGenRequest) {
    return this.http.post<ApiResponse<JobGenResponse>>(environment.apiUrl+ 'jobs/'+uId +'/generate', payload);
  }
  saveDraft(uId: string, payload: Draft) {
    return this.http.put<ApiResponse<Draft>>(environment.apiUrl+ 'jobs/'+uId +'/save-draft',payload);
  }

  loadDrafts(companyId: string) {
    const baseUrl = this.featureFlagService.isMonolith()?environment.monolithUrl: environment.microserviceUrl;
    return this.http.get<ApiResponse<Draft[]>>(baseUrl+ 'jobs/'+companyId +'/list-drafts');
  }

  rewrite(model: EnhancementRequest) {
    const baseUrl = this.featureFlagService.isMonolith()?environment.monolithUrl: environment.microserviceUrl;
    return this.http.put<ApiResponse<EnhancementResponse>>(baseUrl +"jobs/drafts/rewrite", model);
  }

  createJob(model: CreateJobDto) {
    return this.http.post<ApiResponse<Job[]>>(environment.apiUrl + 'jobs', model);
  }
}
