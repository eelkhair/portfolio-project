// Ambient declaration for the Cloudflare Turnstile script injected at runtime.
declare global {
  interface Window {
    turnstile?: {
      render(
        host: HTMLElement,
        options: {
          sitekey: string;
          theme?: 'auto' | 'light' | 'dark';
          callback?: (token: string) => void;
          'expired-callback'?: () => void;
          'error-callback'?: () => void;
        },
      ): string;
      remove(widgetId: string): void;
      reset(widgetId: string): void;
    };
  }
}

export {};
