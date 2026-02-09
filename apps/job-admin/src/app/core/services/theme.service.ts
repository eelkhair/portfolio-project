import {Injectable, signal, effect, computed} from '@angular/core';
import {themeQuartz, colorSchemeDark, colorSchemeLight} from 'ag-grid-community';

const THEME_KEY = 'job-admin-theme';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  isDark = signal(this.getStoredTheme());

  agGridTheme = computed(() =>
    this.isDark()
      ? themeQuartz.withPart(colorSchemeDark)
      : themeQuartz.withPart(colorSchemeLight)
  );

  constructor() {
    effect(() => {
      const dark = this.isDark();
      document.documentElement.classList.toggle('dark', dark);
      localStorage.setItem(THEME_KEY, dark ? 'dark' : 'light');
    });
  }

  toggle(): void {
    this.isDark.update(v => !v);
  }

  private getStoredTheme(): boolean {
    const stored = localStorage.getItem(THEME_KEY);
    if (stored) {
      return stored === 'dark';
    }
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  }
}
