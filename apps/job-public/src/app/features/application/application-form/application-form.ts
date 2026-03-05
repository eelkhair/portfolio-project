import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ResumeData, ProjectDto } from '../../../core/types/resume-data.type';
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
  protected readonly skillsList = signal<string[]>([]);
  protected readonly newSkill = signal('');

  protected readonly form = this.fb.group({
    // Personal Information
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    linkedin: [''],
    portfolio: [''],
    about: [''],

    // Work History
    workHistory: this.fb.array<FormGroup>([]),

    // Education
    education: this.fb.array<FormGroup>([]),

    // Certifications
    certifications: this.fb.array<FormGroup>([]),

    // Projects
    projects: this.fb.array<FormGroup>([]),

    // Cover Letter
    coverLetter: [''],
  });

  protected readonly sections = [
    'Personal Information',
    'Work History',
    'Education',
    'Certifications',
    'Skills & Projects',
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
      if (data.summary) fields.add('about');
      if (data.workHistory?.length) fields.add('workHistory');
      if (data.education?.length) fields.add('education');
      if (data.certifications?.length) fields.add('certifications');
      if (data.projects?.length) fields.add('projects');
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

  get projectsArray(): FormArray {
    return this.form.controls.projects;
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
          about: data.summary ?? '',
        });
        this.skillsList.set(data.skills ?? []);

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

        this.projectsArray.clear();
        if (data.projects?.length) {
          data.projects.forEach(proj => this.addProject(proj));
        }
      } else {
        // Resume switched or cleared — reset all form fields
        this.form.reset();
        this.workHistoryArray.clear();
        this.educationArray.clear();
        this.certificationsArray.clear();
        this.projectsArray.clear();
        this.skillsList.set([]);
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
          about: profile.about ?? '',
        });
        this.skillsList.set(profile.skills ?? []);

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
        if (profile.projects?.length) {
          this.projectsArray.clear();
          profile.projects.forEach(proj => this.addProject(proj));
        }

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

  // --- Projects ---
  addProject(initial?: ProjectDto): void {
    this.projectsArray.push(this.fb.group({
      name: [initial?.name ?? '', Validators.required],
      description: [initial?.description ?? ''],
      technologies: [initial?.technologies?.join(', ') ?? ''],
      url: [initial?.url ?? ''],
    }));
  }

  removeProject(i: number): void {
    this.projectsArray.removeAt(i);
  }

  getProjectTechnologies(i: number): string[] {
    const val = (this.projectsArray.at(i) as FormGroup).controls['technologies'].value ?? '';
    return val.split(',').map((t: string) => t.trim()).filter(Boolean);
  }

  addProjectTechnology(i: number, event: KeyboardEvent | null, inputEl?: HTMLInputElement): void {
    if (event && event.key !== 'Enter') return;
    event?.preventDefault();
    const el = inputEl ?? (event?.target as HTMLInputElement);
    const tech = el.value.trim();
    if (!tech) return;
    const current = this.getProjectTechnologies(i);
    if (!current.includes(tech)) {
      current.push(tech);
      (this.projectsArray.at(i) as FormGroup).controls['technologies'].setValue(current.join(', '));
    }
    el.value = '';
  }

  removeProjectTechnology(i: number, techIndex: number): void {
    const current = this.getProjectTechnologies(i);
    current.splice(techIndex, 1);
    (this.projectsArray.at(i) as FormGroup).controls['technologies'].setValue(current.join(', '));
  }

  // --- Skills ---
  addSkill(): void {
    const skill = this.newSkill().trim();
    if (skill && !this.skillsList().includes(skill)) {
      this.skillsList.update(list => [...list, skill]);
      this.newSkill.set('');
    }
  }

  removeSkill(index: number): void {
    this.skillsList.update(list => list.filter((_, i) => i !== index));
  }

  onSkillKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      this.addSkill();
    }
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

      const projects = (val.projects ?? []).map((proj: any) => ({
        name: proj.name,
        description: proj.description || undefined,
        technologies: (proj.technologies ?? '').split(',').map((t: string) => t.trim()).filter(Boolean),
        url: proj.url || undefined,
      }));

      this.store.submitApplication(
        this.jobId(),
        val.coverLetter ?? '',
        {
          phone: val.phone || undefined,
          linkedin: val.linkedin || undefined,
          portfolio: val.portfolio || undefined,
          about: val.about || undefined,
          skills: this.skillsList(),
          workHistory,
          education,
          certifications,
          projects,
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
          workHistory,
          education,
          certifications,
          skills: this.skillsList(),
          projects,
        },
      );
    }
  }
}
