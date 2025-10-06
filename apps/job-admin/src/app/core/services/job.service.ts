import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {environment} from '../../../environments/environment';
import {Job} from '../types/models/Job';
import {JobGenRequest, JobGenResponse} from '../types/Dtos/JobGen';

@Injectable({ providedIn: 'root' })
export class JobService {
  private http: HttpClient = inject(HttpClient);

  list(companyUId: string){
    return this.http.get<ApiResponse<Job[]>>(environment.apiUrl+ 'jobs/'+companyUId);
  }

  generateDraft(uId: string, payload: JobGenRequest) {
    return this.http.post<ApiResponse<JobGenResponse>>(environment.apiUrl+ 'jobs/'+uId +'/generate', payload);
  }
}
