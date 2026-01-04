import {inject, Injectable} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Company } from '../types/models/Company';
import { Industry } from '../types/models/Industry';
import { CreateCompanyDto } from '../types/Dtos/CreateCompanyDto';
import { ApiResponse } from '../types/Dtos/ApiResponse';
import { FeatureFlagsService } from './feature-flags.service';
import { map, Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class CompanyService {

  private readonly http = inject(HttpClient);
  private readonly featureFlags = inject(FeatureFlagsService);

  // ---- public API ----

  listCompanies(): Observable<ApiResponse<Company[]>> {

    return this.featureFlags.isMonolith()
      ? this.listOData()
      : this.listMicro();
  }

  listIndustries(): Observable<ApiResponse<Industry[]>> {
    return this.featureFlags.isMonolith()
      ? this.listIndustriesOData()
      : this.listIndustriesMicro();
  }

  listMicro(): Observable<ApiResponse<Company[]>> {
    return this.http.get<ApiResponse<Company[]>>(
      `${environment.microserviceUrl}companies`
    );
  }

  listIndustriesMicro(): Observable<ApiResponse<Industry[]>> {
    return this.http.get<ApiResponse<Industry[]>>(
      `${environment.microserviceUrl}industries`
    );
  }

  createCompany(dto: CreateCompanyDto): Observable<ApiResponse<Company>> {
    const base = this.featureFlags.isMonolith()? environment.monolithUrl : environment.microserviceUrl;
    return this.http.post<ApiResponse<Company>>(
      `${base}companies`,
      dto
    );
  }

  // ---- OData ----

  private listOData(): Observable<ApiResponse<Company[]>> {
    return this.http
      .get<ODataResponse<Company>>(`${environment.monolithUrl}odata/companies`)
      .pipe(
        map(res => this.normalizeOData(res))
      );
  }

  private listIndustriesOData(): Observable<ApiResponse<Industry[]>> {
    return this.http
      .get<ODataResponse<Industry>>(`${environment.monolithUrl}odata/industries`)
      .pipe(
        map(res => this.normalizeOData(res))
      );
  }

  private normalizeOData<T extends { id: string; uId?: string }>(
    response: ODataResponse<T>
  ): ApiResponse<T[]> {
    return {
      data: response.value.map(item => ({
        ...item,
        uId: item.uId ?? item.id
      })),
      success: true,
      statusCode: 200
    };
  }

}

export interface ODataResponse<T> {
  '@odata.context'?: string;
  '@odata.count'?: number;
  value: T[];
}
