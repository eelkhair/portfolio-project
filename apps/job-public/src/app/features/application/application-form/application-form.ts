import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ResumeData } from '../../../core/types/resume-data.type';
import { WorkHistoryDto, EducationDto, CertificationDto } from '../../../core/types/application.type';

@Component({
  selector: 'app-application-form',
  imports: [ReactiveFormsModule],
  templateUrl: './application-form.html',
})
export class ApplicationForm {
  protected readonly store = inject(ApplicationStore);
  private readonly fb = inject(FormBuilder);

  resumeData = input<ResumeData | null>(null);
  jobId = input.required<string>();
  resumeId = input<string>('');

  protected readonly activeSection = signal(0);

  protected readonly form = this.fb.group({
    // Personal Information
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    linkedin: [''],
    portfolio: [''],

    // Work History
    workHistory: this.fb.array<FormGroup>([]),

    // Education
    education: this.fb.array<FormGroup>([]),

    // Certifications
    certifications: this.fb.array<FormGroup>([]),

    // Skills
    skills: [''],

    // Cover Letter
    coverLetter: [''],
  });

  protected readonly sections = [
    'Personal Information',
    'Work History',
    'Education',
    'Certifications',
    'Skills',
  ];

  protected readonly aiFields = computed(() => {
    const data = this.resumeData();
    const fields = new Set<string>();
    if (data) {
      if (data.firstName) fields.add('firstName');
      if (data.lastName) fields.add('lastName');
      if (data.email) fields.add('email');
      if (data.phone) fields.add('phone');
      if (data.linkedin) fields.add('linkedin');
      if (data.portfolio) fields.add('portfolio');
      if (data.skills.length > 0) fields.add('skills');
      if (data.workHistory?.length) fields.add('workHistory');
      if (data.education?.length) fields.add('education');
      if (data.certifications?.length) fields.add('certifications');
    }
    return fields;
  });

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
    // Auto-fill from resume parsing — clears form when switching resumes
    effect(() => {
      const data = this.resumeData();
      if (data) {
        this.form.patchValue({
          firstName: data.firstName,
          lastName: data.lastName,
          email: data.email,
          phone: data.phone,
          linkedin: data.linkedin,
          portfolio: data.portfolio,
          skills: data.skills.join(', '),
        });

        this.workHistoryArray.clear();
        if (data.workHistory?.length) {
          data.workHistory.forEach(wh => this.addWorkHistory(wh));
        }

        this.educationArray.clear();
        if (data.education?.length) {
          data.education.forEach(ed => this.addEducation(ed));
        }

        this.certificationsArray.clear();
        if (data.certifications?.length) {
          data.certifications.forEach(cert => this.addCertification(cert));
        }
      } else {
        // Resume switched or cleared — reset all form fields
        this.form.reset();
        this.workHistoryArray.clear();
        this.educationArray.clear();
        this.certificationsArray.clear();
      }
    });

    // Pre-fill from existing profile
    effect(() => {
      const loaded = this.store.profileLoaded();
      const profile = this.store.profile();
      if (loaded && profile && !this.resumeData()) {
        this.form.patchValue({
          firstName: profile.firstName,
          lastName: profile.lastName,
          email: profile.email,
          phone: profile.phone ?? '',
          linkedin: profile.linkedin ?? '',
          portfolio: profile.portfolio ?? '',
          skills: profile.skills.join(', '),
        });
        this.store.profileLoaded.set(false);
      }
    });
  }

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

  goToSection(index: number): void {
    this.activeSection.set(index);
  }

  nextSection(): void {
    if (this.activeSection() < this.sections.length - 1) {
      this.activeSection.update(v => v + 1);
    }
  }

  prevSection(): void {
    if (this.activeSection() > 0) {
      this.activeSection.update(v => v - 1);
    }
  }

  onSubmit(): void {
    if (this.form.valid) {
      const val = this.form.value;

      this.store.submitApplication(
        this.jobId(),
        val.coverLetter ?? '',
        {
          phone: val.phone || undefined,
          linkedin: val.linkedin || undefined,
          portfolio: val.portfolio || undefined,
          skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
        },
        this.resumeId() || undefined,
        {
          personalInfo: {
            firstName: val.firstName ?? '',
            lastName: val.lastName ?? '',
            email: val.email ?? '',
            phone: val.phone || undefined,
            linkedin: val.linkedin || undefined,
            portfolio: val.portfolio || undefined,
          },
          workHistory: (val.workHistory ?? []) as WorkHistoryDto[],
          education: (val.education ?? []) as EducationDto[],
          certifications: (val.certifications ?? []) as CertificationDto[],
          skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
        },
      );
    }
  }
}
