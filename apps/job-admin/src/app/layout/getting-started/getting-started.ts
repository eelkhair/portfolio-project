import {Component, inject, OnInit, signal} from '@angular/core';
import {Dialog} from 'primeng/dialog';
import {Button} from 'primeng/button';
import {AccountService} from '../../core/services/account.service';

@Component({
  selector: 'app-getting-started',
  imports: [Dialog, Button],
  templateUrl: './getting-started.html',
})
export class GettingStarted implements OnInit {
  private accountService = inject(AccountService);

  visible = signal(true);
  displayName = signal('');

  ngOnInit() {
    const u = this.accountService.user();
    this.displayName.set(u?.['preferred_username'] ?? u?.['given_name'] ?? 'demo');
  }

  close() {
    this.visible.set(false);
  }
}
