import { Component, OnInit, inject, signal } from '@angular/core';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { EnhancementRequest } from '../../../core/types/Dtos/EnhancementDto';
import { FormBuilder, FormArray, FormControl, Validators, AbstractControl, ValidationErrors, ReactiveFormsModule } from '@angular/forms';
import { StepperModule } from 'primeng/stepper';
import { Button } from 'primeng/button';
import { Select } from 'primeng/select';
import { InputNumber } from 'primeng/inputnumber';
import { FormsModule } from '@angular/forms';
import {Tag} from 'primeng/tag';
import {Tooltip} from 'primeng/tooltip';
import {InputText} from 'primeng/inputtext';
import {ProgressSpinner} from 'primeng/progressspinner';
import {JobService} from '../../../core/services/job.service';
@Component({
  selector: 'app-job-enhancer',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    StepperModule,
    Button,
    Select,
    InputNumber,
    Tag,
    Tooltip,
    InputText,
    ProgressSpinner,
  ],
  templateUrl: './job-enhancer.html',
  styleUrl: './job-enhancer.css'
})
export class JobEnhancer implements OnInit {
  private fb = inject(FormBuilder);
  private service = inject(JobService);
  options = signal<string[]>([]);
  ref = inject(DynamicDialogRef);
  config = inject(DynamicDialogConfig);
  loading = signal<boolean>(false);

  model = signal<EnhancementRequest | undefined>(undefined);

  // ------------------ FORM SETUP ------------------
  form = this.fb.nonNullable.group({
    aboutRoleChoice: [''],
    tone: ['Neutral', Validators.required],
    formality: ['Neutral'],
    maxWords: [100, [Validators.min(10), Validators.max(2000)]],
    numParagraphs: [1, [Validators.min(1), Validators.max(4)]],
    avoidPhrases: this.fb.array<FormControl<string>>([], [this.maxArrayLength(20)]),
  });

  ngOnInit() {
    const data = this.config.data?.model as EnhancementRequest | undefined;
    if (data) {
      this.model.set(data);
      this.form.controls.maxWords.setValue(data.field==='aboutRole'?800:20)
      this.form.controls.numParagraphs.setValue(data.field==='aboutRole'?2:1)
    }

  }

  // ------------------ VALIDATORS ------------------
  private maxArrayLength(max: number) {
    return (control: AbstractControl): ValidationErrors | null =>
      Array.isArray(control.value) && control.value.length > max
        ? { maxArrayLength: { max } }
        : null;
  }

  // ------------------ ACCESSORS ------------------
  get avoidPhrases(): FormArray<FormControl<string>> {
    return this.form.controls.avoidPhrases as FormArray<FormControl<string>>;
  }

  // ------------------ MUTATORS ------------------
  addAvoidPhrase(value: string = ''): void {
    const v = value.trim();
    if (!v || v.length < 2) return;
    if (this.avoidPhrases.length >= 20) return;

    const exists = this.avoidPhrases.value.some(x => x.toLowerCase() === v.toLowerCase());
    if (exists) return;

    this.avoidPhrases.push(this.fb.control(v, { nonNullable: true }));
    this.avoidPhrases.markAsDirty();
  }

  removeAvoidPhrase(i: number): void {
    this.avoidPhrases.removeAt(i);
    this.avoidPhrases.markAsDirty();
  }

  // ------------------ ACTIONS ------------------
  close(): void {
    const response = this.form.controls.aboutRoleChoice.value;
    this.ref.close(response);
  }

  save(): void {
    if (this.form.invalid) return;
    let newModel = { ...this.model(),
      style:{
        tone: this.form.controls.tone.value,
        formality: this.form.controls.formality.value,
        maxWords: this.form.controls.maxWords.value,
        numParagraphs: this.form.controls.numParagraphs.value,
        avoidPhrases: this.form.controls.avoidPhrases.value.length > 0 ? this.form.controls.avoidPhrases.value : undefined,
      }
    } as EnhancementRequest;
    this.model.set(newModel);
    this.loading.set(true)
    this.service.rewrite(newModel).subscribe({
      next: response => {
        this.loading.set(false);
        this.options.set(response?.data?.options ??[])
        this.form.controls.aboutRoleChoice.setValue(response?.data?.options?.[0]??'')
      }
    });
  }

  onPhraseKeydown(event: KeyboardEvent, input: HTMLInputElement): void {
    if (event.key === 'Enter' || event.key === ',') {
      event.preventDefault();
      const value = input.value.replace(',', '').trim();
      if (value) {
        this.addAvoidPhrase(value);
        input.value = '';
      }
    }
  }
}
