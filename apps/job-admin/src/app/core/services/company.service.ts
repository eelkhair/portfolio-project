import {inject, Injectable} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {Company} from '../types/companies/Company';
import {environment} from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class CompanyService {
  http = inject(HttpClient);

  list(){
    return this.http.get<Company[]>(environment.apiUrl+'companies');
  }
}
