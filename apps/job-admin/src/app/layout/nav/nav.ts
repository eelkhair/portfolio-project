import {Component, inject, OnInit} from '@angular/core';
import {PanelMenu} from 'primeng/panelmenu';

import {NavigationEnd, Router} from '@angular/router';
import {filter} from 'rxjs';
import {NavItems} from './menuItems';

@Component({
  selector: 'app-nav',
  imports: [
    PanelMenu

  ],
  templateUrl: './nav.html',
  styleUrl: './nav.css'
})
export class Nav implements OnInit {
  items = new NavItems().getNavItems().map(i => ({ ...i, expanded: true }))
  router = inject(Router)
  ngOnInit(): void {
    const apply = () => {
      const url = this.router.url;
      this.items.forEach(group => {
        (group.items ?? []).forEach(c => {
          (c as any).styleClass = (typeof c.routerLink === 'string' && url.startsWith(c.routerLink as string))
            ? 'router-active'
            : undefined;
        });
      });
      this.items = this.items.map(i => ({ ...i }));
    };

    apply();
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(apply);
  }

}
