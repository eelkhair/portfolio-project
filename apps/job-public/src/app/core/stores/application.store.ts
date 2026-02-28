import { Injectable, signal } from '@angular/core';
import { of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { MockDataService } from '../services/mock-data.service';
import { ApplicationForm, ApplicationStatus } from '../types/application.type';
import { ParseStatus, ResumeData } from '../types/resume-data.type';

@Injectable({ providedIn: 'root' })
export class ApplicationStore {
  readonly parseStatus = signal<ParseStatus>('idle');
  readonly resumeData = signal<ResumeData | null>(null);
  readonly applicationStatus = signal<ApplicationStatus>('idle');
  readonly fileName = signal('');

  constructor(private dataService: MockDataService) {}

  parseResume(file: File): void {
    this.fileName.set(file.name);
    this.parseStatus.set('uploading');

    // Simulate upload phase
    of(null)
      .pipe(delay(800))
      .subscribe(() => {
        this.parseStatus.set('parsing');

        // Simulate AI parsing
        this.dataService.parseResume().subscribe((data) => {
          this.resumeData.set(data);
          this.parseStatus.set('parsed');
        });
      });
  }

  submitApplication(_form: ApplicationForm): void {
    this.applicationStatus.set('submitting');
    of(null)
      .pipe(delay(1500))
      .subscribe(() => {
        this.applicationStatus.set('submitted');
      });
  }

  reset(): void {
    this.parseStatus.set('idle');
    this.resumeData.set(null);
    this.applicationStatus.set('idle');
    this.fileName.set('');
  }
}
