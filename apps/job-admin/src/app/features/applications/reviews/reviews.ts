import {Component} from '@angular/core';
import {Card} from 'primeng/card';
import {Tag} from 'primeng/tag';
import {Avatar} from 'primeng/avatar';
import {ProgressBar} from 'primeng/progressbar';

interface ReviewCandidate {
  name: string;
  role: string;
  company: string;
  matchScore: number;
  status: 'Pending' | 'Reviewed' | 'Shortlisted' | 'Rejected';
  severity: 'warn' | 'info' | 'success' | 'danger';
  avatar: string;
  skills: string[];
}

@Component({
  selector: 'app-reviews',
  imports: [Card, Tag, Avatar, ProgressBar],
  templateUrl: './reviews.html',
})
export class Reviews {
  stats = {
    pendingReview: 7,
    avgMatchScore: 82,
    shortlisted: 3
  };

  candidates: ReviewCandidate[] = [
    {name: 'Sarah Chen', role: 'Staff .NET Engineer', company: 'Nexus Analytics', matchScore: 94, status: 'Shortlisted', severity: 'success', avatar: 'SC', skills: ['.NET', 'DDD', 'CQRS', 'Azure']},
    {name: 'Thomas Wright', role: 'Principal Engineer', company: 'Nexus Analytics', matchScore: 91, status: 'Shortlisted', severity: 'success', avatar: 'TW', skills: ['.NET', 'Microservices', 'Kubernetes']},
    {name: 'Elena Vasquez', role: 'Staff .NET Engineer', company: 'Nexus Analytics', matchScore: 87, status: 'Reviewed', severity: 'info', avatar: 'EV', skills: ['.NET', 'Angular', 'SQL Server']},
    {name: 'David Kim', role: 'Senior ML Engineer', company: 'Paradise Hospitality', matchScore: 83, status: 'Shortlisted', severity: 'success', avatar: 'DK', skills: ['Python', 'PyTorch', 'pgvector']},
    {name: 'Marcus Rivera', role: 'Senior DevOps Engineer', company: 'Paradise Hospitality', matchScore: 78, status: 'Reviewed', severity: 'info', avatar: 'MR', skills: ['Docker', 'Terraform', 'GitHub Actions']},
    {name: 'Priya Sharma', role: 'DevOps Engineer', company: 'GreenWave Energy', matchScore: 72, status: 'Pending', severity: 'warn', avatar: 'PS', skills: ['Kubernetes', 'Helm', 'Grafana']},
    {name: 'James O\'Brien', role: 'Principal Engineer', company: 'Nexus Analytics', matchScore: 68, status: 'Pending', severity: 'warn', avatar: 'JO', skills: ['.NET', 'SignalR', 'Redis']},
    {name: 'Lisa Nakamura', role: 'Staff .NET Engineer', company: 'Urban Retail', matchScore: 45, status: 'Rejected', severity: 'danger', avatar: 'LN', skills: ['Java', 'Spring Boot', 'AWS']},
  ];
}
