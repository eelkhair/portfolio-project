import {MenuItem} from 'primeng/api';
import {inject} from '@angular/core';
import {AccountService} from '../../core/services/account.service';

export class NavItems{
  accountService = inject(AccountService);

  getNavItems(){
    const groups = this.accountService.groups();
    const isAdmin = groups.some(g => g.replace(/^\//, '') === 'Admins');
    const isSystemAdmin = groups.some(g => g.replace(/^\//, '') === 'SystemAdmins');
    const NAV_ITEMS: MenuItem[] = [
      {
        label: 'Dashboard', icon: 'pi pi-chart-bar', routerLink: '/dashboard'
      },

      {
        label: 'Jobs', icon: 'pi pi-briefcase',
        items: [
          { label: 'All Jobs', icon: 'pi pi-list', routerLink: '/jobs', routerLinkActiveOptions: {exact: true} },
          { label: 'Create Job', icon: 'pi pi-plus', routerLink: '/jobs/new' },
          { label: 'Drafts', icon: 'pi pi-file', routerLink: '/jobs/drafts' },
        ]
      },

      {
        label: 'Companies', icon: 'pi pi-building', visible: isAdmin,
        items: [
          { label: 'All Companies', icon: 'pi pi-list', routerLink: '/companies', routerLinkActiveOptions: {exact: true} },
        ]
      },
      {
        label: 'Applications', icon: 'pi pi-inbox',
        items: [
          { label: 'Pipeline', icon: 'pi pi-sitemap', routerLink: '/applications' },
          { label: 'Reviews', icon: 'pi pi-check-square', routerLink: '/applications/reviews' },
        ]
      },

      {
        label: 'Users & Access', icon: 'pi pi-users',
        visible: false, // toggled true if user has 'admin'
        items: [
          { label: 'Users', icon: 'pi pi-user', routerLink: '/access/users' },
          { label: 'Roles & Permissions', icon: 'pi pi-shield', routerLink: '/access/roles' },
          { label: 'Organizations', icon: 'pi pi-sitemap', routerLink: '/access/organizations' },
        ]
      },
      {
        label: 'Settings', icon: 'pi pi-cog', visible: isSystemAdmin,
        items: [
          { label: 'AI Provider', icon: 'pi pi-microchip-ai', routerLink: '/settings/ai-provider' },
          { label: 'Application Mode', icon: 'pi pi-arrows-h', routerLink: '/settings/application-mode' },
          { label: 'Embedding Management', icon: 'pi pi-database', routerLink: '/settings/embedding-management' },
          { label: 'Feature Flags', icon: 'pi pi-flag', routerLink: '/settings/feature-flags' },
        ]
      },

      { label: 'Audit Logs', icon: 'pi pi-history', routerLink: '/audit', visible: false },
    ];
    return NAV_ITEMS;
  }

}

