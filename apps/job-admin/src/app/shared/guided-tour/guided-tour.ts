import {Component, effect, inject, OnDestroy, signal, ViewEncapsulation} from '@angular/core';
import {AccountService} from '../../core/services/account.service';
import {GettingStartedService} from '../../layout/getting-started/getting-started.service';

const STORAGE_KEY = 'job-admin-tour-shown';
const SHOW_DELAY = 2000;
const TOUR_DURATION = 10000;

interface Highlight {
  label: string;
  description: string;
  rect: DOMRect;
  position: 'below' | 'left' | 'right';
}

@Component({
  selector: 'app-guided-tour',
  encapsulation: ViewEncapsulation.None,
  templateUrl: './guided-tour.html',
  styles: `
    .tour-overlay {
      position: fixed;
      inset: 0;
      z-index: 9999;
      cursor: pointer;
      animation: tourFadeIn 0.4s ease-out;
    }
    .tour-overlay.tour-fade-out {
      animation: tourFadeOut 0.4s ease-out forwards;
    }
    .tour-backdrop {
      position: absolute;
      inset: 0;
      width: 100%;
      height: 100%;
    }
    .tour-ring {
      position: absolute;
      transform: translate(-50%, -50%);
      border: 2px solid rgba(99, 102, 241, 0.6);
      border-radius: 8px;
      animation: tourPulse 1.5s ease-in-out infinite;
      pointer-events: none;
    }
    .tour-tooltip {
      position: absolute;
      width: 220px;
      background: white;
      border-radius: 10px;
      padding: 12px 14px;
      box-shadow: 0 8px 24px rgba(0, 0, 0, 0.25);
      pointer-events: none;
      z-index: 10000;
    }
    .tour-tooltip-label {
      font-size: 13px;
      font-weight: 700;
      color: #1e293b;
      margin-bottom: 2px;
    }
    .tour-tooltip-desc {
      font-size: 12px;
      color: #64748b;
      line-height: 1.4;
    }
    .tour-tooltip::after {
      content: '';
      position: absolute;
      width: 10px;
      height: 10px;
      background: white;
      transform: rotate(45deg);
    }
    .tour-tooltip-below::after {
      top: -5px;
      left: 24px;
    }
    .tour-tooltip-left::after {
      right: -5px;
      top: 14px;
    }
    .tour-tooltip-right::after {
      left: -5px;
      top: 14px;
    }
    .tour-dismiss {
      position: fixed;
      bottom: 32px;
      left: 50%;
      transform: translateX(-50%);
      color: rgba(255, 255, 255, 0.7);
      font-size: 13px;
      pointer-events: none;
      z-index: 10000;
    }
    @keyframes tourPulse {
      0%, 100% { opacity: 0.4; transform: translate(-50%, -50%) scale(1); }
      50% { opacity: 1; transform: translate(-50%, -50%) scale(1.15); }
    }
    @keyframes tourFadeIn {
      from { opacity: 0; }
      to { opacity: 1; }
    }
    @keyframes tourFadeOut {
      from { opacity: 1; }
      to { opacity: 0; }
    }
  `,
})
export class GuidedTour implements OnDestroy {
  private accountService = inject(AccountService);
  private gettingStarted = inject(GettingStartedService);
  private timers: ReturnType<typeof setTimeout>[] = [];

  visible = signal(false);
  fadingOut = signal(false);
  highlights = signal<Highlight[]>([]);

  constructor() {
    effect(() => {
      if (this.accountService.hasInitialized() && !sessionStorage.getItem(STORAGE_KEY)) {
        const t = setTimeout(() => this.show(), SHOW_DELAY);
        this.timers.push(t);
      }
    });
  }

  private show() {
    const targets: { selector: string; label: string; description: string; position: 'below' | 'left' | 'right' }[] = [
      {
        selector: 'app-mode-toggle',
        label: 'Toggle Mode',
        description: 'Switch between Monolith & Microservices to compare architectures.',
        position: 'below',
      },
      {
        selector: '.pi-question-circle',
        label: 'How to Explore',
        description: 'Open the guide anytime to see what features to try.',
        position: 'below',
      },
      {
        selector: '.chat-fab',
        label: 'AI Chat',
        description: 'Ask the AI to create jobs, list companies, or generate drafts.',
        position: 'left',
      },
      {
        selector: '.debug-fab',
        label: 'Debug Bar',
        description: 'View distributed traces in Jaeger & Grafana.',
        position: 'right',
      },
    ];

    const found: Highlight[] = [];
    for (const t of targets) {
      const el = document.querySelector(t.selector);
      if (el) {
        found.push({
          label: t.label,
          description: t.description,
          rect: el.getBoundingClientRect(),
          position: t.position,
        });
      }
    }

    if (found.length === 0) return;

    this.highlights.set(found);
    this.visible.set(true);
    sessionStorage.setItem(STORAGE_KEY, 'true');

    const t = setTimeout(() => this.fadeOut(), TOUR_DURATION);
    this.timers.push(t);
  }

  dismiss() {
    this.fadeOut();
  }

  private fadeOut() {
    if (!this.visible() || this.fadingOut()) return;
    this.fadingOut.set(true);
    const t = setTimeout(() => {
      this.visible.set(false);
      this.fadingOut.set(false);
      this.gettingStarted.visible.set(true);
    }, 400);
    this.timers.push(t);
  }

  tooltipLeft(h: Highlight): number {
    if (h.position === 'below') return h.rect.left;
    if (h.position === 'right') return h.rect.right + 14;
    return h.rect.left - 230;
  }

  tooltipTop(h: Highlight): number {
    if (h.position === 'below') return h.rect.bottom + 14;
    return h.rect.top - 4;
  }

  ngOnDestroy() {
    this.timers.forEach(t => clearTimeout(t));
  }
}
