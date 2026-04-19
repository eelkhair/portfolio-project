import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../types/Dtos/ApiResponse';
import { Observable } from 'rxjs';
import { ApplicationDetail, ApplicationListItem, ApplicationStatus, PaginatedResponse } from '../types/models/Application';
import { ActivityLogger } from './activity-logger.service';

export interface ApplicationFilters {
  status?: ApplicationStatus;
  jobId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
  includeMatchScores?: boolean;
}

@Injectable({ providedIn: 'root' })
export class ApplicationService {
  private readonly http = inject(HttpClient);
  private readonly logger = inject(ActivityLogger);
  private readonly baseUrl = `${environment.gatewayUrl}api/applications`;

  list(filters?: ApplicationFilters): Observable<ApiResponse<PaginatedResponse<ApplicationListItem>>> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.jobId) params = params.set('jobId', filters.jobId);
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters?.includeMatchScores) params = params.set('includeMatchScores', 'true');

    return this.http.get<ApiResponse<PaginatedResponse<ApplicationListItem>>>(this.baseUrl, { params });
  }

  getDetail(id: string): Observable<ApiResponse<ApplicationDetail>> {
    return this.http.get<ApiResponse<ApplicationDetail>>(`${this.baseUrl}/${id}`);
  }

  updateStatus(id: string, status: ApplicationStatus): Observable<ApiResponse<ApplicationListItem>> {
    return this.http
      .patch<ApiResponse<ApplicationListItem>>(`${this.baseUrl}/${id}/status`, { status })
      .pipe(this.logger.trace('application status update', () => ({ id, status })));
  }

  batchUpdateStatus(ids: string[], status: ApplicationStatus): Observable<ApiResponse<ApplicationListItem[]>> {
    return this.http
      .patch<ApiResponse<ApplicationListItem[]>>(
        `${this.baseUrl}/batch-status`,
        { applicationIds: ids, status },
      )
      .pipe(this.logger.trace('application batch update', () => ({ count: ids.length, status })));
  }
}
