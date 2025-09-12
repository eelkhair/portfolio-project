import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Company} from '../types/models/Company';
import {environment} from '../../../environments/environment';
import {Industry} from '../types/models/Industry';
import {CreateCompanyDto} from '../types/Dtos/CreateCompanyDto';
import {ApiResponse} from '../types/Dtos/ApiResponse';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  http = inject(HttpClient);


  list(){
    return this.http.get<Company[]>(environment.apiUrl+'companies');
  }
  listIndustries(){
    return this.http.get<Industry[]>(environment.apiUrl+'industries');
  }

  createCompany(company: CreateCompanyDto) {
    return this.http.post<ApiResponse<Company>>(environment.apiUrl+'companies', company);
  }
}
