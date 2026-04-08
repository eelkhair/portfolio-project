import {Component, inject, OnInit, signal} from '@angular/core';
import {Dialog} from 'primeng/dialog';
import {Button} from 'primeng/button';
import {AccountService} from '../../core/services/account.service';

const STORAGE_KEY = 'job-admin-getting-started-shown';

@Component({
  selector: 'app-getting-started',
  imports: [Dialog, Button],
  templateUrl: './getting-started.html',
})
export class GettingStarted implements OnInit {
  private accountService = inject(AccountService);

  visible = signal(false);
  displayName = signal('');

  ngOnInit() {
    const u = this.accountService.user();
    this.displayName.set(u?.['preferred_username'] ?? u?.['given_name'] ?? 'demo');

    if (!sessionStorage.getItem(STORAGE_KEY)) {
      setTimeout(() => this.visible.set(true), 7000);
    }
  }

  close() {
    sessionStorage.setItem(STORAGE_KEY, 'true');
    this.visible.set(false);
  }
}
