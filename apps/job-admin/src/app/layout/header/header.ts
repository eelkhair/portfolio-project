import {Component, inject, OnInit, signal} from '@angular/core';
import {Button} from 'primeng/button';
import {Menu} from 'primeng/menu';
import {Tooltip} from 'primeng/tooltip';
import {AccountService} from '../../core/services/account.service';
import {TitleCasePipe} from '@angular/common';
import {MenuItem} from 'primeng/api';
import {ThemeService} from '../../core/services/theme.service';
import {environment} from '../../../environments/environment';
import {ModeToggle} from './mode-toggle';

@Component({
  selector: 'app-header',
  imports: [
    Button,
    Menu,
    TitleCasePipe,
    Tooltip,
    ModeToggle
  ],
  templateUrl: './header.html',
  styleUrl: './header.css'
})
export class Header implements OnInit {
  accountService = inject(AccountService);
  themeService = inject(ThemeService);
  menuItems = signal<MenuItem[]>([]);
  displayName = signal('');
  roleName = signal('');
  envName = environment.envName;

  ngOnInit() {
    const u = this.accountService.user();
    if (u) {
      this.displayName.set((u['given_name'] ?? u['name']) || u['name'] || u['preferred_username'] || u['email'] || 'User');
    }
    const groups = this.accountService.groups().map(g => g.replace(/^\//, ''));
    if (groups.includes('SystemAdmins')) this.roleName.set('System Admin');
    else if (groups.includes('Admins')) this.roleName.set('Admin');
    else if (groups.some(g => g.includes('CompanyAdmins'))) this.roleName.set('Company Admin');
    else if (groups.some(g => g.includes('Recruiters'))) this.roleName.set('Recruiter');
    else if (groups.includes('Applicants')) this.roleName.set('Applicant');
    else this.roleName.set('User');
    this.menuItems.set([
      {
        label: 'Logout',
        icon: 'pi pi-sign-out',
        command: () => this.accountService.logout()
      }
    ]);
  }
}
