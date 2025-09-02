import {Component, effect, inject, OnInit, signal} from '@angular/core';
import {Button} from 'primeng/button';
import {Menu} from 'primeng/menu';
import {AccountService} from '../../core/services/account.service';
import {TitleCasePipe} from '@angular/common';
import {MenuItem} from 'primeng/api';
import {Router} from '@angular/router';

@Component({
  selector: 'app-header',
  imports: [
    Button,
    Menu,
    TitleCasePipe
  ],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit {
  accountService = inject(AccountService);
  router = inject(Router);
  menuItems = signal<MenuItem[]>([]);
  displayName = signal('')

  ngOnInit() {
    this.accountService.auth.user$.subscribe(u => {
      if (u) {
        this.displayName.set(u.name || u.nickname || u.email || 'User');
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
    })
  }
}
