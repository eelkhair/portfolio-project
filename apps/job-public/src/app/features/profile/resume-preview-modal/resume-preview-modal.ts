import { Component, HostListener, inject } from '@angular/core';
import { DomSanitizer, SafeHtml, SafeResourceUrl } from '@angular/platform-browser';
import { ProfileStore } from '../../../core/stores/profile.store';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';

@Component({
  selector: 'app-resume-preview-modal',
  imports: [LoadingSpinner],
  templateUrl: './resume-preview-modal.html',
})
export class ResumePreviewModal {
  protected readonly store = inject(ProfileStore);
  private readonly sanitizer = inject(DomSanitizer);

  @HostListener('document:keydown.escape')
  onEscapeKey(): void {
    if (this.store.previewResume()) {
      this.onClose();
    }
  }

  isPdf(): boolean {
    return this.store.previewResume()?.contentType === 'application/pdf';
  }

  safePreviewUrl(): SafeResourceUrl | null {
    const url = this.store.previewUrl();
    return url ? this.sanitizer.bypassSecurityTrustResourceUrl(url) : null;
  }

  safePreviewHtml(): SafeHtml | null {
    const html = this.store.previewHtml();
    return html ? this.sanitizer.bypassSecurityTrustHtml(html) : null;
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  onBackdropClick(event: MouseEvent): void {
    if (event.target === event.currentTarget) {
      this.onClose();
    }
  }

  onClose(): void {
    this.store.closePreview();
  }

  onDownload(): void {
    const resume = this.store.previewResume();
    if (resume) {
      this.store.downloadResume(resume);
    }
  }
}
