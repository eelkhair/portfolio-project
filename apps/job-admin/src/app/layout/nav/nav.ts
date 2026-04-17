import {Component, computed, inject, OnInit, signal} from '@angular/core';
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
  private readonly navSource = new NavItems();
  private readonly router = inject(Router);
  private readonly currentUrl = signal(this.router.url);

  /**
   * Reactive menu items: recomputes when feature flags or user groups change
   * (NavItems.getNavItems() reads those signals internally) or when the router
   * URL changes (for active-link styling). Previously a plain field initializer
   * snapshotted flag state exactly once before SignalR delivered anything —
   * leaving gated items like Contact permanently hidden until a full reload.
   */
  readonly items = computed(() => {
    const url = this.currentUrl();
    const items = this.navSource.getNavItems();
    const markActive = (items: any[]): void => {
      items.forEach(item => {
        if (typeof item.routerLink === 'string') {
          item.styleClass = url.startsWith(item.routerLink) ? 'router-active' : undefined;
        }
        if (item.items) {
          markActive(item.items);
        }
      });
    };
    markActive(items);
    return items;
  });

  ngOnInit(): void {
    this.router.events
      .pipe(filter(e => e instanceof NavigationEnd))
      .subscribe(() => this.currentUrl.set(this.router.url));
  }
}
