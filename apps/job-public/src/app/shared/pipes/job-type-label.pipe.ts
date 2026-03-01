import { Pipe, PipeTransform } from '@angular/core';

const labels: Record<string, string> = {
  fullTime: 'Full Time',
  partTime: 'Part Time',
  contract: 'Contract',
  internship: 'Internship',
};

@Pipe({ name: 'jobTypeLabel' })
export class JobTypeLabelPipe implements PipeTransform {
  transform(value: string): string {
    return labels[value] ?? value;
  }
}
