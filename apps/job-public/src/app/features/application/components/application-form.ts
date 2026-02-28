import { Component, computed, effect, inject, input } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApplicationStore } from '../../../core/stores/application.store';
import { ResumeData } from '../../../core/types/resume-data.type';

@Component({
  selector: 'app-application-form',
  imports: [ReactiveFormsModule],
  template: `
    <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-6">
      <div class="grid grid-cols-1 gap-6 sm:grid-cols-2">
        <!-- Full Name -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Full Name
            @if (aiFields().has('fullName')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="text" formControlName="fullName" class="input-field" placeholder="Your full name" />
        </div>

        <!-- Email -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Email
            @if (aiFields().has('email')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="email" formControlName="email" class="input-field" placeholder="you@example.com" />
        </div>

        <!-- Phone -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Phone
            @if (aiFields().has('phone')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="tel" formControlName="phone" class="input-field" placeholder="+1 (555) 000-0000" />
        </div>

        <!-- LinkedIn -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            LinkedIn
            @if (aiFields().has('linkedin')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="text" formControlName="linkedin" class="input-field" placeholder="linkedin.com/in/you" />
        </div>

        <!-- Portfolio -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Portfolio
            @if (aiFields().has('portfolio')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="text" formControlName="portfolio" class="input-field" placeholder="yoursite.com" />
        </div>

        <!-- Skills -->
        <div>
          <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
            Key Skills
            @if (aiFields().has('skills')) {
              <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
            }
          </label>
          <input type="text" formControlName="skills" class="input-field" placeholder="TypeScript, React, Node.js" />
        </div>
      </div>

      <!-- Experience -->
      <div>
        <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
          Experience Summary
          @if (aiFields().has('experience')) {
            <span class="ml-1 text-xs text-purple-500" title="AI auto-filled">&#10024;</span>
          }
        </label>
        <textarea
          formControlName="experience"
          rows="3"
          class="input-field"
          placeholder="Brief summary of your relevant experience..."
        ></textarea>
      </div>

      <!-- Cover Letter -->
      <div>
        <label class="mb-1.5 block text-sm font-medium text-slate-700 dark:text-slate-300">
          Cover Letter
        </label>
        <textarea
          formControlName="coverLetter"
          rows="5"
          class="input-field"
          placeholder="Why are you interested in this position..."
        ></textarea>
      </div>

      @if (aiFields().size > 0) {
        <div class="flex items-center gap-2 rounded-lg bg-purple-50 px-4 py-3 text-sm dark:bg-purple-900/20">
          <span class="text-purple-500">&#10024;</span>
          <span class="text-purple-700 dark:text-purple-300">
            {{ aiFields().size }} fields were auto-filled by AI from your resume.
          </span>
        </div>
      }

      <button
        type="submit"
        [disabled]="form.invalid || store.applicationStatus() === 'submitting'"
        class="btn-primary w-full disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:translate-y-0"
      >
        @if (store.applicationStatus() === 'submitting') {
          <div class="mr-2 h-4 w-4 rounded-full border-2 border-white/30 border-t-white animate-spin"></div>
          Submitting...
        } @else {
          Submit Application
        }
      </button>
    </form>
  `,
})
export class ApplicationForm {
  protected readonly store = inject(ApplicationStore);
  private readonly fb = inject(FormBuilder);

  resumeData = input<ResumeData | null>(null);

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
  }

  onSubmit(): void {
    if (this.form.valid) {
      const val = this.form.value;
      this.store.submitApplication({
        fullName: val.fullName ?? '',
        email: val.email ?? '',
        phone: val.phone ?? '',
        linkedin: val.linkedin ?? '',
        portfolio: val.portfolio ?? '',
        experience: val.experience ?? '',
        coverLetter: val.coverLetter ?? '',
        skills: (val.skills ?? '').split(',').map((s: string) => s.trim()).filter(Boolean),
      });
    }
  }
}
