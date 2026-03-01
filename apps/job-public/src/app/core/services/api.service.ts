import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Job, JobType } from '../types/job.type';
import { Company } from '../types/company.type';
import { Stats } from '../types/stats.type';
import { ApiResponse } from '../types/api-response.type';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl + 'public';

  getJobs(): Observable<Job[]> {
    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/jobs`)
      .pipe(map((res) => res.data ?? []));
  }

  searchJobs(filters: { query?: string; jobType?: string; location?: string; limit?: number }): Observable<Job[]> {
    let params = new HttpParams();
    if (filters.query) params = params.set('query', filters.query);
    if (filters.jobType) params = params.set('jobType', filters.jobType);
    if (filters.location) params = params.set('location', filters.location);
    if (filters.limit) params = params.set('limit', filters.limit);

    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/jobs/search`, { params })
      .pipe(map((res) => res.data ?? []));
  }

  getJobById(id: string): Observable<Job | undefined> {
    return this.http
      .get<ApiResponse<Job>>(`${this.baseUrl}/jobs/${id}`)
      .pipe(map((res) => res.data));
  }

  getLatestJobs(count = 6): Observable<Job[]> {
    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/jobs/latest`, {
        params: new HttpParams().set('count', count),
      })
      .pipe(map((res) => res.data ?? []));
  }

  getSimilarJobs(jobId: string, companyUId: string, jobType: JobType): Observable<Job[]> {
    const params = new HttpParams().set('companyUId', companyUId).set('jobType', jobType);
    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/jobs/${jobId}/similar`, { params })
      .pipe(map((res) => res.data ?? []));
  }

  getCompanies(): Observable<Company[]> {
    return this.http
      .get<ApiResponse<Company[]>>(`${this.baseUrl}/companies`)
      .pipe(map((res) => res.data ?? []));
  }

  getCompanyById(id: string): Observable<Company | undefined> {
    return this.http
      .get<ApiResponse<Company>>(`${this.baseUrl}/companies/${id}`)
      .pipe(map((res) => res.data));
  }

  getCompanyJobs(companyId: string): Observable<Job[]> {
    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/companies/${companyId}/jobs`)
      .pipe(map((res) => res.data ?? []));
  }

  getStats(): Observable<Stats> {
    return this.http
      .get<ApiResponse<Stats>>(`${this.baseUrl}/stats`)
      .pipe(map((res) => res.data ?? { jobCount: 0, companyCount: 0 }));
  }
}
