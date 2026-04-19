import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Job, JobType, MatchingJob } from '../types/job.type';
import { Company } from '../types/company.type';
import { Stats } from '../types/stats.type';
import { ApiResponse, PaginatedList } from '../types/api-response.type';
import { SubmitApplicationRequest, ApplicationResponse } from '../types/application.type';
import { ResumeData, ResumeResponse, UserProfile, UserProfileRequest } from '../types/resume-data.type';
import { ActivityLogger } from './activity-logger.service';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly logger = inject(ActivityLogger);
  private readonly baseUrl = environment.apiUrl + 'public';
  private readonly applicantUrl = environment.apiUrl + 'applicant';
  private readonly resumesUrl = environment.apiUrl + 'resumes';

  getJobs(page = 1, pageSize = 10): Observable<PaginatedList<Job>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http
      .get<ApiResponse<PaginatedList<Job>>>(`${this.baseUrl}/jobs`, { params })
      .pipe(map((res) => res.data ?? { items: [], page: 1, pageSize, totalCount: 0, totalPages: 0, hasPreviousPage: false, hasNextPage: false }));
  }

  searchJobs(filters: { query?: string; jobType?: string; location?: string; limit?: number }): Observable<Job[]> {
    let params = new HttpParams();
    if (filters.query) params = params.set('query', filters.query);
    if (filters.jobType) params = params.set('jobType', filters.jobType);
    if (filters.location) params = params.set('location', filters.location);
    if (filters.limit) params = params.set('limit', filters.limit);

    return this.http
      .get<ApiResponse<Job[]>>(`${this.baseUrl}/jobs/search`, { params })
      .pipe(
        map((res) => res.data ?? []),
        this.logger.trace('job search', (results) => ({
          hasQuery: !!filters.query,
          hasJobType: !!filters.jobType,
          hasLocation: !!filters.location,
          count: results.length,
        })),
      );
  }

  getJobById(id: string): Observable<Job | undefined> {
    return this.http
      .get<ApiResponse<Job>>(`${this.baseUrl}/jobs/${id}`)
      .pipe(
        map((res) => res.data),
        this.logger.trace('job view', () => ({ id })),
      );
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
      .pipe(map((res) => res.data ?? { jobCount: 0, companyCount: 0, applicationCount: 0 }));
  }

  // --- Authenticated applicant endpoints ---

  getProfile(): Observable<UserProfile | null> {
    return this.http
      .get<ApiResponse<UserProfile>>(`${this.applicantUrl}/profile`)
      .pipe(map((res) => res.data ?? null));
  }

  upsertProfile(profile: UserProfileRequest): Observable<UserProfile> {
    return this.http
      .put<ApiResponse<UserProfile>>(`${this.applicantUrl}/profile`, profile)
      .pipe(
        map((res) => res.data!),
        this.logger.trace('profile upsert'),
      );
  }

  getApplications(): Observable<ApplicationResponse[]> {
    return this.http
      .get<ApiResponse<ApplicationResponse[]>>(`${this.applicantUrl}/applications`)
      .pipe(map((res) => res.data ?? []));
  }

  submitApplication(request: SubmitApplicationRequest): Observable<ApplicationResponse> {
    return this.http
      .post<ApiResponse<ApplicationResponse>>(`${this.applicantUrl}/applications`, request)
      .pipe(
        map((res) => res.data!),
        this.logger.trace('application submit', (r) => ({ id: r.id })),
      );
  }

  // --- Resume endpoints ---

  uploadResume(file: File, currentPage?: string): Observable<ResumeResponse> {
    const formData = new FormData();
    formData.append('file', file);
    let params = new HttpParams();
    if (currentPage) params = params.set('currentPage', currentPage);
    return this.http
      .post<ApiResponse<ResumeResponse>>(`${this.resumesUrl}`, formData, { params })
      .pipe(
        map((res) => res.data!),
        this.logger.trace('resume upload', (r) => ({
          id: r.id,
          fileName: file.name,
          fileSize: file.size,
          contentType: file.type,
          currentPage,
        })),
      );
  }

  getResumes(): Observable<ResumeResponse[]> {
    return this.http
      .get<ApiResponse<ResumeResponse[]>>(`${this.resumesUrl}`)
      .pipe(map((res) => res.data ?? []));
  }

  getResumeParsedContent(id: string, traceParent?: string): Observable<ResumeData | null> {
    const options = traceParent ? { headers: new HttpHeaders({ traceparent: traceParent }) } : {};
    return this.http
      .get<ApiResponse<ResumeData | null>>(`${this.resumesUrl}/${id}/parsed-content`, options)
      .pipe(map((res) => res.data ?? null));
  }

  setDefaultResume(id: string): Observable<void> {
    return this.http
      .patch<void>(`${this.resumesUrl}/${id}/default`, null)
      .pipe(this.logger.trace('resume set default', () => ({ id })));
  }

  deleteResume(id: string): Observable<void> {
    return this.http
      .delete<void>(`${this.resumesUrl}/${id}`)
      .pipe(this.logger.trace('resume delete', () => ({ id })));
  }

  reEmbedResume(id: string): Observable<void> {
    return this.http
      .post<void>(`${this.resumesUrl}/${id}/re-embed`, null)
      .pipe(this.logger.trace('resume re-embed', () => ({ id })));
  }

  getMatchingJobs(limit = 10, traceParent?: string): Observable<MatchingJob[]> {
    const params = new HttpParams().set('limit', limit);
    const headers = traceParent ? new HttpHeaders({ traceparent: traceParent }) : undefined;
    return this.http
      .get<ApiResponse<MatchingJob[]>>(`${this.resumesUrl}/jobs/matching`, { params, headers })
      .pipe(map((res) => res.data ?? []));
  }

  downloadResumeBlob(id: string): Observable<Blob> {
    return this.http
      .get(`${this.resumesUrl}/${id}/download`, {
        params: { inline: 'true' },
        responseType: 'blob',
      })
      .pipe(this.logger.trace('resume download', (b) => ({ id, size: b.size })));
  }
}
