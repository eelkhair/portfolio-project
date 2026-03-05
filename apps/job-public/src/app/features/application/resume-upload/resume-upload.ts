import { Component, effect, inject, OnInit, output, signal } from '@angular/core';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ProfileStore } from '../../../core/stores/profile.store';
import { RouterLink } from '@angular/router';
import { ALL_RESUME_SECTIONS, ResumeSection, SECTION_LABELS, SectionStatus } from '../../../core/types/resume-data.type';

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
  private waitingForUpload = false;

  readonly resumeIdChange = output<string>();

  constructor() {
    // Detect newly uploaded resume and ask user before auto-filling
    effect(() => {
      const resumes = this.profileStore.resumes();
      if (
        this.mode() === 'upload' &&
        this.waitingForUpload &&
        resumes.length > this.resumesCountBeforeUpload
      ) {
        this.waitingForUpload = false;
        const newResume = resumes[0];
        this.selectedResumeId.set(newResume.id);
        this.resumeIdChange.emit(newResume.id);
        this.store.resumeId.set(newResume.id);
        this.store.parseStatus.set('ready');
        this.resumesCountBeforeUpload = resumes.length;
      }
    });

    // Bridge: handle upload error
    effect(() => {
      const error = this.profileStore.uploadError();
      if (error && (this.store.parseStatus() === 'uploading' || this.store.parseStatus() === 'parsing')) {
        this.store.parseStatus.set('error');
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
  }

  confirmResumeSelection(): void {
    const id = this.selectedResumeId();
    this.resumeIdChange.emit(id);

    if (id) {
      const resume = this.profileStore.resumes().find((r) => r.id === id);
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
    const data = this.store.resumeData() ?? this.store.pendingResumeData();
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

  acceptAutoFill(): void {
    const id = this.selectedResumeId();
    if (id) {
      this.store.initProgressiveParse(id);
    }
  }

  skipAutoFill(): void {
    this.store.parseStatus.set('parsed');
  }

  // --- Section tracking helpers for template ---
  readonly parseSections = ALL_RESUME_SECTIONS;
  readonly parseSectionLabels = SECTION_LABELS;

  sectionStatus(section: ResumeSection): SectionStatus {
    return this.store.sectionStatuses()[section];
  }

  private uploadFile(file: File): void {
    // Single upload via ProfileStore — bridge effects in constructor
    // push parsed content to ApplicationStore automatically
    this.resumesCountBeforeUpload = this.profileStore.resumes().length;
    this.waitingForUpload = true;
    this.store.fileName.set(file.name);
    this.store.parseStatus.set('uploading');
    this.profileStore.uploadResume(file);
  }
}
