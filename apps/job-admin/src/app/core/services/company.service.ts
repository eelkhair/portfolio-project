import {inject, Injectable} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Company } from '../types/models/Company';
import { Industry } from '../types/models/Industry';
import { CreateCompanyDto } from '../types/Dtos/CreateCompanyDto';
import { UpdateCompanyDto } from '../types/Dtos/UpdateCompanyDto';
import { ApiResponse } from '../types/Dtos/ApiResponse';
import { map, Observable } from 'rxjs';
import { ActivityLogger } from './activity-logger.service';

@Injectable({ providedIn: 'root' })
export class CompanyService {

  private readonly http = inject(HttpClient);
  private readonly logger = inject(ActivityLogger);

  listCompanies(): Observable<ApiResponse<Company[]>> {
    return this.http.get<any>(`${environment.gatewayUrl}api/companies`).pipe(
      map(res => this.normalize<Company>(res))
    );
  }

  listIndustries(): Observable<ApiResponse<Industry[]>> {
    return this.http.get<any>(`${environment.gatewayUrl}api/industries`).pipe(
      map(res => this.normalize<Industry>(res))
    );
  }

  createCompany(dto: CreateCompanyDto): Observable<ApiResponse<Company>> {
    return this.http
      .post<ApiResponse<Company>>(`${environment.gatewayUrl}api/companies`, dto)
      .pipe(this.logger.trace('company create', (r) => ({ id: r.data?.id })));
  }

  updateCompany(id: string, dto: UpdateCompanyDto): Observable<ApiResponse<Company>> {
    return this.http
      .put<ApiResponse<Company>>(`${environment.gatewayUrl}api/companies/${id}`, dto)
      .pipe(this.logger.trace('company update', () => ({ id })));
  }

  private normalize<T extends { id: string; uId?: string }>(
    response: any
  ): ApiResponse<T[]> {
    // ApiResponse format (admin API)
    if (response?.data) return response;
    // Plain array (monolith via non-OData route) or OData envelope
    const items: T[] = Array.isArray(response) ? response : response?.value ?? [];
    return {
      data: items.map(item => ({ ...item, uId: item.uId ?? item.id })),
      success: true,
      statusCode: 200
    };
  }

}
