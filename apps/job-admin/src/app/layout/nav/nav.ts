import {Component, inject, OnInit} from '@angular/core';
import {Menubar} from 'primeng/menubar';
import {NavigationEnd, Router} from '@angular/router';
import {filter} from 'rxjs';
import {NavItems} from './menuItems';

@Component({
  selector: 'app-nav',
  imports: [
    Menubar
  ],
  templateUrl: './nav.html',
  styleUrl: './nav.css'
})
export class Nav implements OnInit {
  items = new NavItems().getNavItems();
  router = inject(Router);

  ngOnInit(): void {
    const apply = () => {
      const url = this.router.url;
      const markActive = (items: any[]) => {
        items.forEach(item => {
          if (typeof item.routerLink === 'string') {
            item.styleClass = url.startsWith(item.routerLink) ? 'router-active' : undefined;
          }
          if (item.items) {
            markActive(item.items);
          }
        });
      };
      markActive(this.items);
      this.items = [...this.items];
    };

    apply();
    this.router.events.pipe(filter(e => e instanceof NavigationEnd)).subscribe(apply);
  }
}
