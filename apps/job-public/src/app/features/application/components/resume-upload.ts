import { Component, inject } from '@angular/core';
import { ApplicationStore } from '../../../core/stores/application.store';

@Component({
  selector: 'app-resume-upload',
  template: `
    @switch (store.parseStatus()) {
      @case ('idle') {
        <div
          class="card cursor-pointer border-2 border-dashed border-slate-300 p-10 text-center transition-colors hover:border-primary-400 dark:border-slate-600 dark:hover:border-primary-500"
          (click)="fileInput.click()"
          (dragover)="onDragOver($event)"
          (dragleave)="onDragLeave($event)"
          (drop)="onDrop($event)"
          [class.border-primary-400]="isDragging"
          [class.bg-primary-50]="isDragging"
          role="button"
          tabindex="0"
          (keydown.enter)="fileInput.click()"
        >
          <div class="flex flex-col items-center">
            <div class="ai-gradient flex h-14 w-14 items-center justify-center rounded-2xl">
              <svg
                class="h-7 w-7 text-white"
                fill="none"
                viewBox="0 0 24 24"
                stroke="currentColor"
                stroke-width="2"
              >
                <path
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  d="M3 16.5v2.25A2.25 2.25 0 005.25 21h13.5A2.25 2.25 0 0021 18.75V16.5m-13.5-9L12 3m0 0l4.5 4.5M12 3v13.5"
                />
              </svg>
            </div>
            <div class="mt-4">
              <span class="ai-gradient rounded-full px-3 py-1 text-xs font-semibold text-white">
                AI-Powered
              </span>
            </div>
            <p class="mt-3 text-sm font-medium text-slate-900 dark:text-white">
              Drop your resume here or click to upload
            </p>
            <p class="mt-1 text-xs text-slate-500 dark:text-slate-400">
              PDF, DOCX, or TXT (max 5MB)
            </p>
          </div>
          <input
            #fileInput
            type="file"
            accept=".pdf,.docx,.doc,.txt"
            class="hidden"
            (change)="onFileSelected($event)"
          />
        </div>
      }
      @case ('uploading') {
        <div class="card p-10 text-center">
          <div class="mx-auto h-10 w-10 rounded-full border-4 border-slate-200 border-t-primary-600 animate-spin dark:border-slate-700"></div>
          <p class="mt-4 font-medium text-slate-900 dark:text-white">Uploading resume...</p>
          <p class="mt-1 text-sm text-slate-500">{{ store.fileName() }}</p>
        </div>
      }
      @case ('parsing') {
        <div class="card p-10 text-center">
          <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-2xl ai-gradient animate-pulse-slow">
            <svg
              class="h-7 w-7 text-white"
              fill="none"
              viewBox="0 0 24 24"
              stroke="currentColor"
              stroke-width="2"
            >
              <path
                stroke-linecap="round"
                stroke-linejoin="round"
                d="M9.813 15.904L9 18.75l-.813-2.846a4.5 4.5 0 00-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 003.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 003.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 00-3.09 3.09z"
              />
            </svg>
          </div>
          <p class="mt-4 font-medium text-slate-900 dark:text-white">AI is parsing your resume...</p>
          <p class="mt-1 text-sm text-slate-500 dark:text-slate-400">
            Extracting skills, experience, and contact info
          </p>
          <div class="mx-auto mt-4 h-1.5 w-48 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-700">
            <div class="h-full animate-shimmer ai-gradient rounded-full"></div>
          </div>
        </div>
      }
      @case ('parsed') {
        @if (store.resumeData(); as data) {
          <div class="card p-6">
            <div class="flex items-center gap-3">
              <div class="flex h-10 w-10 items-center justify-center rounded-full bg-green-100 dark:bg-green-900/30">
                <svg
                  class="h-6 w-6 text-green-600 dark:text-green-400"
                  fill="none"
                  viewBox="0 0 24 24"
                  stroke="currentColor"
                  stroke-width="2"
                >
                  <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                </svg>
              </div>
              <div>
                <p class="font-semibold text-slate-900 dark:text-white">Resume parsed successfully</p>
                <p class="text-sm text-slate-500 dark:text-slate-400">{{ store.fileName() }}</p>
              </div>
            </div>
            <div class="mt-4 grid grid-cols-1 gap-3 text-sm sm:grid-cols-2">
              @for (field of parsedFields; track field.label) {
                <div class="flex items-center gap-2">
                  <svg
                    class="h-4 w-4 shrink-0 text-green-500"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                    stroke-width="2"
                  >
                    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
                  </svg>
                  <span class="text-slate-600 dark:text-slate-400">{{ field.label }}: </span>
                  <span class="font-medium text-slate-900 dark:text-white">{{ field.value }}</span>
                </div>
              }
            </div>
            @if (data.skills.length > 0) {
              <div class="mt-4 flex flex-wrap gap-1.5">
                @for (skill of data.skills; track skill) {
                  <span
                    class="rounded-md bg-primary-50 px-2 py-0.5 text-xs font-medium text-primary-700 dark:bg-primary-900/20 dark:text-primary-400"
                  >
                    {{ skill }}
                  </span>
                }
              </div>
            }
          </div>
        }
      }
      @case ('error') {
        <div class="card border-red-200 p-6 text-center dark:border-red-800">
          <p class="text-red-600 dark:text-red-400">Failed to parse resume. Please try again.</p>
        </div>
      }
    }
  `,
})
export class ResumeUpload {
  protected readonly store = inject(ApplicationStore);
  protected isDragging = false;

  get parsedFields() {
    const data = this.store.resumeData();
    if (!data) return [];
    return [
      { label: 'Name', value: data.fullName },
      { label: 'Email', value: data.email },
      { label: 'Phone', value: data.phone },
      { label: 'LinkedIn', value: data.linkedin },
    ];
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files?.[0]) {
      this.store.parseResume(input.files[0]);
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
      this.store.parseResume(event.dataTransfer.files[0]);
    }
  }
}
