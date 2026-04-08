import {Component, inject, input, OnInit} from '@angular/core';
import {DatePipe} from '@angular/common';
import {Router} from '@angular/router';
import {Button} from 'primeng/button';
import {Card} from 'primeng/card';
import {Divider} from 'primeng/divider';
import {Tag} from 'primeng/tag';
import {JobsStore} from '../jobs.store';

@Component({
  selector: 'app-job-detail',
  imports: [DatePipe, Button, Card, Divider, Tag],
  templateUrl: './job-detail.html',
})
export class JobDetail implements OnInit {
  store = inject(JobsStore);
  private router = inject(Router);
  id = input.required<string>();

  ngOnInit() {
    this.store.selectJob(this.id());
  }

  back() {
    this.router.navigate(['/jobs']);
  }
}
