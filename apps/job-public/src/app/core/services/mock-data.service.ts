import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { MOCK_RESUME_DATA } from '../data/mock-data';
import { ResumeData } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class MockDataService {
  parseResume(): Observable<ResumeData> {
    return of(MOCK_RESUME_DATA).pipe(delay(2000));
  }
}
