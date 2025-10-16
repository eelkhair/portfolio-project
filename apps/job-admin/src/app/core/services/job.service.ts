import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {Job} from '../types/models/Job';
import {JobGenRequest, JobGenResponse} from '../types/Dtos/JobGen';
import {Draft} from '../types/Dtos/draft';

@Injectable({ providedIn: 'root' })
export class JobService {
  private http: HttpClient = inject(HttpClient);

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
    return this.http.get<ApiResponse<Draft[]>>(environment.apiUrl+ 'jobs/'+companyId +'/list-drafts');

  }
}
