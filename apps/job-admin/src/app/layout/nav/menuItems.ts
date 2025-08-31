import {MenuItem} from 'primeng/api';

export const NAV_ITEMS: MenuItem[] = [
  {
    label: 'Dashboard', icon: 'pi pi-chart-bar', routerLink: '/dashboard'
  },

  {
    label: 'Jobs', icon: 'pi pi-briefcase',
    items: [
      { label: 'All Jobs', icon: 'pi pi-list', routerLink: '/jobs' },
      { label: 'Create Job', icon: 'pi pi-plus', routerLink: '/jobs/new' },
      { label: 'Drafts', icon: 'pi pi-file', routerLink: '/jobs/drafts' },
    ]
  },

  {
    label: 'Companies', icon: 'pi pi-building',
    items: [
      { label: 'All Companies', icon: 'pi pi-list', routerLink: '/companies' },
      { label: 'Create Company', icon: 'pi pi-plus', routerLink: '/companies/new' },
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
    label: 'Settings', icon: 'pi pi-cog',
    items: [
      { label: 'Profile', icon: 'pi pi-id-card', routerLink: '/settings/profile' },
      { label: 'App Settings', icon: 'pi pi-sliders-h', routerLink: '/settings/app' },
    ]
  },

  { label: 'Audit Logs', icon: 'pi pi-history', routerLink: '/audit', visible: false },
  { label: 'Support', icon: 'pi pi-question-circle', routerLink: '/support' },
];
