import {Component, inject, OnInit, signal} from '@angular/core';
import { RouterOutlet } from '@angular/router';
import {HttpClient} from '@angular/common/http';
import {AuthButtonComponent} from './AuthButton';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, AuthButtonComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly title = signal('job-admin');
  protected http = inject(HttpClient);

  ngOnInit() {
    this.http.get('https://job-api.eelkhair.net/companies').subscribe({
      next: data => { console.log(data); },
    })
  }
}
