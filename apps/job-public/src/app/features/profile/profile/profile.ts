import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog';
import { ProfileStore } from '../../../core/stores/profile.store';
import { AccountService } from '../../../core/services/account.service';
import { ResumePreviewModal } from '../resume-preview-modal/resume-preview-modal';
import { ResumeResponse } from '../../../core/types/resume-data.type';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinner, DatePipe, ResumePreviewModal, ConfirmDialog],
  templateUrl: './profile.html',
})
export class Profile implements OnInit {
  protected readonly store = inject(ProfileStore);
  protected readonly account = inject(AccountService);
  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.group({
    phone: [''],
    linkedin: [''],
    portfolio: [''],
    skills: [''],
    experience: [''],
    preferredLocation: [''],
    preferredJobType: [''],
  });

  constructor() {
    // Pre-fill form when profile loads
    effect(() => {
      const profile = this.store.profile();
      if (profile) {
        this.form.patchValue({
          phone: profile.phone ?? '',
          linkedin: profile.linkedin ?? '',
          portfolio: profile.portfolio ?? '',
          skills: profile.skills.join(', '),
          experience: profile.experience ?? '',
          preferredLocation: profile.preferredLocation ?? '',
          preferredJobType: profile.preferredJobType ?? '',
        });
      }
    });
  }

  ngOnInit(): void {
    this.store.loadProfile();
    this.store.loadResumes();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.length) {
      this.store.uploadResume(input.files[0]);
      input.value = '';
    }
  }

  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files[0];
    if (file) {
      this.store.uploadResume(file);
    }
  }

  onPreviewResume(resume: ResumeResponse): void {
    this.store.openPreview(resume);
  }

  protected readonly showDeleteConfirm = signal(false);
  private deleteResumeId = '';

  onDeleteResume(id: string): void {
    this.deleteResumeId = id;
    this.showDeleteConfirm.set(true);
  }

  onConfirmDelete(): void {
    this.showDeleteConfirm.set(false);
    this.store.deleteResume(this.deleteResumeId);
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return bytes + ' B';
    if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
    return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
  }

  onSave(): void {
    const val = this.form.value;

    this.store.saveProfile({
      phone: val.phone || undefined,
      linkedin: val.linkedin || undefined,
      portfolio: val.portfolio || undefined,
      experience: val.experience || undefined,
      skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
      preferredLocation: val.preferredLocation || undefined,
      preferredJobType: val.preferredJobType || undefined,
    });
  }
}
