import {Component} from '@angular/core';
import {Card} from 'primeng/card';
import {Tag} from 'primeng/tag';
import {Avatar} from 'primeng/avatar';

interface PipelineCandidate {
  name: string;
  role: string;
  company: string;
  daysAgo: number;
  avatar: string;
}

interface PipelineStage {
  name: string;
  severity: 'info' | 'warn' | 'success' | 'danger' | 'secondary' | 'contrast';
  candidates: PipelineCandidate[];
}

@Component({
  selector: 'app-pipeline',
  imports: [Card, Tag, Avatar],
  templateUrl: './pipeline.html',
})
export class Pipeline {
  stages: PipelineStage[] = [
    {
      name: 'Applied',
      severity: 'info',
      candidates: [
        {name: 'Sarah Chen', role: 'Staff .NET Engineer', company: 'Nexus Analytics', daysAgo: 1, avatar: 'SC'},
        {name: 'Marcus Rivera', role: 'Senior DevOps Engineer', company: 'Paradise Hospitality', daysAgo: 2, avatar: 'MR'},
        {name: 'Aisha Patel', role: 'ML Engineer', company: 'GreenWave Energy', daysAgo: 3, avatar: 'AP'},
        {name: 'James O\'Brien', role: 'Principal Engineer', company: 'Nexus Analytics', daysAgo: 4, avatar: 'JO'},
      ]
    },
    {
      name: 'Screening',
      severity: 'warn',
      candidates: [
        {name: 'Elena Vasquez', role: 'Staff .NET Engineer', company: 'Nexus Analytics', daysAgo: 5, avatar: 'EV'},
        {name: 'David Kim', role: 'Senior ML Engineer', company: 'Paradise Hospitality', daysAgo: 6, avatar: 'DK'},
        {name: 'Priya Sharma', role: 'DevOps Engineer', company: 'GreenWave Energy', daysAgo: 7, avatar: 'PS'},
      ]
    },
    {
      name: 'Interview',
      severity: 'success',
      candidates: [
        {name: 'Thomas Wright', role: 'Principal Engineer', company: 'Nexus Analytics', daysAgo: 10, avatar: 'TW'},
        {name: 'Lisa Nakamura', role: 'Staff .NET Engineer', company: 'Urban Retail', daysAgo: 12, avatar: 'LN'},
      ]
    },
    {
      name: 'Offered',
      severity: 'contrast',
      candidates: [
        {name: 'Alex Petrov', role: 'Senior DevOps Engineer', company: 'Nexus Analytics', daysAgo: 15, avatar: 'AP'},
      ]
    }
  ];
}
