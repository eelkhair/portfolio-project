import { Component, effect, inject, OnInit, output, signal } from '@angular/core';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ProfileStore } from '../../../core/stores/profile.store';
import { RouterLink } from '@angular/router';

type ResumeMode = 'select' | 'upload';

@Component({
  selector: 'app-resume-upload',
  imports: [RouterLink],
  templateUrl: './resume-upload.html',
})
export class ResumeUpload implements OnInit {
  protected readonly store = inject(ApplicationStore);
  protected readonly profileStore = inject(ProfileStore);
  protected isDragging = false;

  protected readonly mode = signal<ResumeMode>('upload');
  protected readonly selectedResumeId = signal('');
  private resumesCountBeforeUpload = 0;

  readonly resumeIdChange = output<string>();

  constructor() {
    // Auto-select newly uploaded resume once it appears in the profile list
    effect(() => {
      const resumes = this.profileStore.resumes();
      if (
        this.mode() === 'upload' &&
        resumes.length > this.resumesCountBeforeUpload &&
        this.resumesCountBeforeUpload > 0
      ) {
        // New resume was prepended — auto-select it
        const newResume = resumes[0];
        this.selectedResumeId.set(newResume.id);
        this.resumeIdChange.emit(newResume.id);
        this.resumesCountBeforeUpload = resumes.length;
      }
    });
  }

  ngOnInit(): void {
    this.profileStore.loadResumes();
  }

  setMode(m: ResumeMode): void {
    this.mode.set(m);
    if (m === 'upload') {
      this.selectedResumeId.set('');
      this.resumeIdChange.emit('');
    }
    if (m === 'select') {
      // Clear AI parse data when switching to select mode
      this.store.resetParse();
    }
  }

  onResumeSelected(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedResumeId.set(select.value);
    this.resumeIdChange.emit(select.value);

    // Trigger AI auto-fill for selected resume
    if (select.value) {
      const resume = this.profileStore.resumes().find((r) => r.id === select.value);
      if (resume) {
        this.store.loadParsedContent(resume.id, resume.originalFileName);
      }
    } else {
      this.store.resetParse();
    }
  }

  removeUpload(): void {
    // Delete from profile if it was saved
    const resumeId = this.selectedResumeId();
    if (resumeId) {
      this.profileStore.deleteResume(resumeId);
    }
    this.store.resetParse();
    this.selectedResumeId.set('');
    this.resumeIdChange.emit('');
  }

  get parsedFields() {
    const data = this.store.resumeData();
    if (!data) return [];
    return [
      { label: 'Name', value: `${data.firstName} ${data.lastName}`.trim() },
      { label: 'Email', value: data.email },
      { label: 'Phone', value: data.phone },
      { label: 'LinkedIn', value: data.linkedin },
    ];
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) {
      this.uploadFile(input.files[0]);
    }
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    if (event.dataTransfer?.files[0]) {
      this.uploadFile(event.dataTransfer.files[0]);
    }
  }

  private uploadFile(file: File): void {
    // Save to profile (adds to existing resumes list)
    this.resumesCountBeforeUpload = this.profileStore.resumes().length;
    this.profileStore.uploadResume(file);

    // AI parse for form auto-fill
    this.store.parseResume(file);
  }
}
