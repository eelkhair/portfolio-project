import {Component, inject, OnInit, signal} from '@angular/core';
import {Drawer} from 'primeng/drawer';
import {Router} from '@angular/router';
import {AccountService} from '../../core/services/account.service';
import {GettingStartedService} from './getting-started.service';

const STORAGE_KEY = 'job-admin-getting-started-shown';

@Component({
  selector: 'app-getting-started',
  imports: [Drawer],
  templateUrl: './getting-started.html',
})
export class GettingStarted implements OnInit {
  private accountService = inject(AccountService);
  private router = inject(Router);
  private gettingStartedService = inject(GettingStartedService);

  visible = this.gettingStartedService.visible;
  displayName = signal('');

  ngOnInit() {
    const u = this.accountService.user();
    this.displayName.set(u?.['preferred_username'] ?? u?.['given_name'] ?? 'demo');
  }

  close() {
    sessionStorage.setItem(STORAGE_KEY, 'true');
    this.visible.set(false);
  }

  toggle() {
    this.visible.update(v => !v);
  }

  navigateTo(route: string) {
    this.close();
    this.router.navigateByUrl(route);
  }
}
