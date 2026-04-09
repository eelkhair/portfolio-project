import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {ApiResponse} from '../types/Dtos/ApiResponse';
import {DashboardResponse} from '../types/Dtos/DashboardResponse';
import {environment} from '../../../environments/environment';
import {map, Observable} from 'rxjs';

@Injectable({providedIn: 'root'})
export class DashboardService {
  private http = inject(HttpClient);

  private get baseUrl(): string {
    return environment.gatewayUrl;
  }

  getDashboard(): Observable<ApiResponse<DashboardResponse>> {
    return this.http.get<any>(`${this.baseUrl}api/dashboard`).pipe(
      map(res => this.normalize(res))
    );
  }

  private normalize(response: any): ApiResponse<DashboardResponse> {
    if (response?.data) return response;
    return {data: response, success: true, statusCode: 200};
  }
}
