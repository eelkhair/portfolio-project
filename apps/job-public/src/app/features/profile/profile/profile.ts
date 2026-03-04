import { Component, effect, inject, OnInit, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { LoadingSpinner } from '../../../shared/components/loading-spinner';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog';
import { ProfileStore } from '../../../core/stores/profile.store';
import { AccountService } from '../../../core/services/account.service';
import { ResumePreviewModal } from '../resume-preview-modal/resume-preview-modal';
import { ResumeResponse } from '../../../core/types/resume-data.type';
import { WorkHistoryDto, EducationDto, CertificationDto } from '../../../core/types/application.type';
import { Validators } from '@angular/forms';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, RouterLink, LoadingSpinner, DatePipe, ResumePreviewModal, ConfirmDialog],
  templateUrl: './profile.html',
})
export class Profile implements OnInit {
  protected readonly store = inject(ProfileStore);
  protected readonly account = inject(AccountService);
  private readonly fb = inject(FormBuilder);

  protected readonly activeSection = signal(0);

  protected readonly sections = [
    'Contact & Links',
    'Work History',
    'Education',
    'Certifications',
    'Skills & Preferences',
  ];

  protected readonly form = this.fb.group({
    phone: [''],
    linkedin: [''],
    portfolio: [''],
    skills: [''],
    preferredLocation: [''],
    preferredJobType: [''],
    workHistory: this.fb.array<FormGroup>([]),
    education: this.fb.array<FormGroup>([]),
    certifications: this.fb.array<FormGroup>([]),
  });

  get parsedFields() {
    const data = this.store.pendingParsedContent();
    if (!data) return [];
    return [
      { label: 'Phone', value: data.phone },
      { label: 'LinkedIn', value: data.linkedin },
      { label: 'Portfolio', value: data.portfolio },
    ];
  }

  get workHistoryArray(): FormArray {
    return this.form.controls.workHistory;
  }

  get educationArray(): FormArray {
    return this.form.controls.education;
  }

  get certificationsArray(): FormArray {
    return this.form.controls.certifications;
  }

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
          preferredLocation: profile.preferredLocation ?? '',
          preferredJobType: profile.preferredJobType ?? '',
        });

        if (profile.workHistory?.length) {
          this.workHistoryArray.clear();
          profile.workHistory.forEach(wh => this.addWorkHistory(wh));
        }
        if (profile.education?.length) {
          this.educationArray.clear();
          profile.education.forEach(ed => this.addEducation(ed));
        }
        if (profile.certifications?.length) {
          this.certificationsArray.clear();
          profile.certifications.forEach(cert => this.addCertification(cert));
        }
      }
    });

    // Apply parsed content only after user confirms
    effect(() => {
      const status = this.store.profileParseStatus();
      if (status === 'applied') {
        const data = this.store.pendingParsedContent();
        if (data) {
          this.form.patchValue({
            phone: data.phone || this.form.value.phone,
            linkedin: data.linkedin || this.form.value.linkedin,
            portfolio: data.portfolio || this.form.value.portfolio,
            skills: data.skills.length > 0 ? data.skills.join(', ') : this.form.value.skills,
          });

          if (data.workHistory?.length) {
            this.workHistoryArray.clear();
            data.workHistory.forEach(wh => this.addWorkHistory(wh));
          }
          if (data.education?.length) {
            this.educationArray.clear();
            data.education.forEach(ed => this.addEducation(ed));
          }
          if (data.certifications?.length) {
            this.certificationsArray.clear();
            data.certifications.forEach(cert => this.addCertification(cert));
          }
          // Clear pending data after applying
          this.store.pendingParsedContent.set(null);
        }
      }
    });
  }

  ngOnInit(): void {
    this.store.loadProfile();
    this.store.loadResumes();
  }

  // --- Work History ---
  addWorkHistory(initial?: WorkHistoryDto): void {
    this.workHistoryArray.push(this.fb.group({
      company: [initial?.company ?? '', Validators.required],
      jobTitle: [initial?.jobTitle ?? '', Validators.required],
      startDate: [initial?.startDate ?? '', Validators.required],
      endDate: [initial?.endDate ?? ''],
      description: [initial?.description ?? ''],
      isCurrent: [initial?.isCurrent ?? false],
    }));
  }

  removeWorkHistory(i: number): void {
    this.workHistoryArray.removeAt(i);
  }

  // --- Education ---
  addEducation(initial?: EducationDto): void {
    this.educationArray.push(this.fb.group({
      institution: [initial?.institution ?? '', Validators.required],
      degree: [initial?.degree ?? '', Validators.required],
      fieldOfStudy: [initial?.fieldOfStudy ?? ''],
      startDate: [initial?.startDate ?? '', Validators.required],
      endDate: [initial?.endDate ?? ''],
    }));
  }

  removeEducation(i: number): void {
    this.educationArray.removeAt(i);
  }

  // --- Certifications ---
  addCertification(initial?: CertificationDto): void {
    this.certificationsArray.push(this.fb.group({
      name: [initial?.name ?? '', Validators.required],
      issuingOrganization: [initial?.issuingOrganization ?? ''],
      issueDate: [initial?.issueDate ?? ''],
      expirationDate: [initial?.expirationDate ?? ''],
      credentialId: [initial?.credentialId ?? ''],
    }));
  }

  removeCertification(i: number): void {
    this.certificationsArray.removeAt(i);
  }

  // --- Section nav ---
  goToSection(index: number): void {
    this.activeSection.set(index);
  }

  // --- Resume ---
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

  onSetDefault(id: string): void {
    this.store.setDefaultResume(id);
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

    // Sanitize dates: empty strings → undefined so the backend receives null instead of ""
    const workHistory = (val.workHistory ?? []).map((wh: any) => ({
      ...wh,
      startDate: wh.startDate || undefined,
      endDate: wh.endDate || undefined,
    })) as WorkHistoryDto[];

    const education = (val.education ?? []).map((ed: any) => ({
      ...ed,
      startDate: ed.startDate || undefined,
      endDate: ed.endDate || undefined,
    })) as EducationDto[];

    const certifications = (val.certifications ?? []).map((cert: any) => ({
      ...cert,
      issueDate: cert.issueDate || undefined,
      expirationDate: cert.expirationDate || undefined,
    })) as CertificationDto[];

    this.store.saveProfile({
      phone: val.phone || undefined,
      linkedin: val.linkedin || undefined,
      portfolio: val.portfolio || undefined,
      skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
      preferredLocation: val.preferredLocation || undefined,
      preferredJobType: val.preferredJobType || undefined,
      workHistory,
      education,
      certifications,
    });
  }
}
