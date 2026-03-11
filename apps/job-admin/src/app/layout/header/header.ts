import {Component, inject, OnInit, signal} from '@angular/core';
import {Button} from 'primeng/button';
import {Menu} from 'primeng/menu';
import {Tooltip} from 'primeng/tooltip';
import {AccountService} from '../../core/services/account.service';
import {TitleCasePipe} from '@angular/common';
import {MenuItem} from 'primeng/api';
import {Router} from '@angular/router';
import {ThemeService} from '../../core/services/theme.service';

@Component({
  selector: 'app-header',
  imports: [
    Button,
    Menu,
    TitleCasePipe,
    Tooltip
  ],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit {
  accountService = inject(AccountService);
  router = inject(Router);
  themeService = inject(ThemeService);
  menuItems = signal<MenuItem[]>([]);
  displayName = signal('')

  ngOnInit() {
    const u = this.accountService.user();
    if (u) {
      this.displayName.set((u['given_name'] ?? u['name']) || u['name'] || u['preferred_username'] || u['email'] || 'User');
    }
    this.menuItems.set([
      {
        label: 'Profile',
        icon: 'pi pi-id-card',
        command: () => this.router.navigateByUrl('/settings/profile')
      },
      {
        separator: true
      },
      {
        label: 'Logout',
        icon: 'pi pi-sign-out',
        command: () => this.accountService.logout()
      }
    ]);
  }
}
