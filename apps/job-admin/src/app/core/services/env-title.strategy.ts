import { inject, Injectable } from '@angular/core';
import { Title } from '@angular/platform-browser';
import { RouterStateSnapshot, TitleStrategy } from '@angular/router';
import { environment } from '../../../environments/environment';

@Injectable()
export class EnvTitleStrategy extends TitleStrategy {
  private readonly title = inject(Title);
  private readonly appName = 'JobAdmin';
  private readonly env = environment.envName;

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const pageTitle = this.buildTitle(snapshot);
    const parts = [this.appName];

    if (pageTitle) {
      parts.push(pageTitle);
    }

    if (this.env !== 'PROD') {
      parts.push(`[${this.env}]`);
    }

    this.title.setTitle(parts.join(' · '));
  }
}
