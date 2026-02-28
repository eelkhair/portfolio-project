import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { COMPANIES, JOBS, MOCK_RESUME_DATA } from '../data/mock-data';
import { Company } from '../types/company.type';
import { Job } from '../types/job.type';
import { ResumeData } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class MockDataService {
  getJobs(): Observable<Job[]> {
    return of(JOBS).pipe(delay(300));
  }

  getJobById(id: string): Observable<Job | undefined> {
    return of(JOBS.find((j) => j.id === id)).pipe(delay(200));
  }

  searchJobs(query: string, type: string, location: string): Observable<Job[]> {
    const q = query.toLowerCase();
    const t = type.toLowerCase();
    const l = location.toLowerCase();

    const filtered = JOBS.filter((job) => {
      const matchesQuery =
        !q ||
        job.title.toLowerCase().includes(q) ||
        job.companyName.toLowerCase().includes(q) ||
        job.skills.some((s) => s.toLowerCase().includes(q));
      const matchesType = !t || job.type.toLowerCase() === t;
      const matchesLocation = !l || job.location.toLowerCase().includes(l);
      return matchesQuery && matchesType && matchesLocation;
    });

    return of(filtered).pipe(delay(300));
  }

  getSimilarJobs(job: Job): Observable<Job[]> {
    const similar = JOBS.filter(
      (j) => j.id !== job.id && (j.companyId === job.companyId || j.type === job.type),
    ).slice(0, 3);
    return of(similar).pipe(delay(200));
  }

  getCompanies(): Observable<Company[]> {
    return of(COMPANIES).pipe(delay(300));
  }

  getCompanyById(id: string): Observable<Company | undefined> {
    return of(COMPANIES.find((c) => c.id === id)).pipe(delay(200));
  }

  getJobsByCompany(companyId: string): Observable<Job[]> {
    return of(JOBS.filter((j) => j.companyId === companyId)).pipe(delay(200));
  }

  getJobCount(): number {
    return JOBS.length;
  }

  getCompanyCount(): number {
    return COMPANIES.length;
  }

  getFeaturedJobs(): Observable<Job[]> {
    return of(JOBS.filter((j) => j.featured).slice(0, 6)).pipe(delay(300));
  }

  parseResume(): Observable<ResumeData> {
    return of(MOCK_RESUME_DATA).pipe(delay(2000));
  }
}
