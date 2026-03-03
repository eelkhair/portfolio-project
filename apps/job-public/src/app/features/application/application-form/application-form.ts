import { Component, computed, effect, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ResumeData } from '../../../core/types/resume-data.type';

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

  protected readonly form = this.fb.group({
    fullName: ['', Validators.required],
    email: ['', [Validators.required, Validators.email]],
    phone: [''],
    linkedin: [''],
    portfolio: [''],
    experience: [''],
    coverLetter: [''],
    skills: [''],
  });

  protected readonly aiFields = computed(() => {
    const data = this.resumeData();
    const fields = new Set<string>();
    if (data) {
      if (data.fullName) fields.add('fullName');
      if (data.email) fields.add('email');
      if (data.phone) fields.add('phone');
      if (data.linkedin) fields.add('linkedin');
      if (data.portfolio) fields.add('portfolio');
      if (data.experience) fields.add('experience');
      if (data.skills.length > 0) fields.add('skills');
    }
    return fields;
  });

  constructor() {
    // Auto-fill from resume parsing
    effect(() => {
      const data = this.resumeData();
      if (data) {
        this.form.patchValue({
          fullName: data.fullName,
          email: data.email,
          phone: data.phone,
          linkedin: data.linkedin,
          portfolio: data.portfolio,
          experience: data.experience,
          skills: data.skills.join(', '),
        });
      }
    });

    // Pre-fill from existing profile (once only, to avoid overwriting user edits)
    effect(() => {
      const loaded = this.store.profileLoaded();
      const profile = this.store.profile();
      if (loaded && profile && !this.resumeData()) {
        this.form.patchValue({
          fullName: `${profile.firstName} ${profile.lastName}`.trim(),
          email: profile.email,
          phone: profile.phone ?? '',
          linkedin: profile.linkedin ?? '',
          portfolio: profile.portfolio ?? '',
          experience: profile.experience ?? '',
          skills: profile.skills.join(', '),
        });
        // Untrack further changes by marking as loaded
        this.store.profileLoaded.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.form.valid) {
      const val = this.form.value;

      // Single sequential call: saves profile, then submits application
      this.store.submitApplication(
        this.jobId(),
        val.coverLetter ?? '',
        {
          phone: val.phone || undefined,
          linkedin: val.linkedin || undefined,
          portfolio: val.portfolio || undefined,
          experience: val.experience || undefined,
          skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
        },
        this.resumeId() || undefined,
      );
    }
  }
}
