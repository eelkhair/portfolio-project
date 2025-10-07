import {Component, computed, effect, inject, OnInit} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {InputText} from 'primeng/inputtext';
import {Textarea} from 'primeng/textarea';
import {Button, ButtonDirective} from 'primeng/button';
import {FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {JobType} from '../../../core/types/Dtos/CreateJobRequest';
import {Select} from 'primeng/select';
import {Tooltip} from 'primeng/tooltip';
import {JobGenerate} from '../job-generate/job-generate';

@Component({
  selector: 'app-job-upsert',
  imports: [
    CompanySelection,
    InputText,
    Textarea,
    Button,
    ReactiveFormsModule,
    Select,
    ButtonDirective,
    Tooltip,
    JobGenerate
  ],
  templateUrl: './job-upsert.html',
  styleUrl: './job-upsert.css'
})
export class JobUpsert implements OnInit {
  store = inject(JobsStore)
  private fb = inject(FormBuilder)
  company = computed(() => this.store.selectedCompany());

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    aboutRole: ['', [Validators.required, Validators.minLength(20)]],
    location: ['', Validators.required],
    jobType: this.fb.nonNullable.control<JobType>('FullTime', Validators.required),
    salaryRange: this.fb.control<string | null>(null),
    responsibilities: this.fb.array<FormControl<string>>([]),
    qualifications: this.fb.array<FormControl<string>>([]),
  });

  jobTypes: { label: string; value: JobType }[] = [
    { label: 'Full time', value: 'FullTime' },
    { label: 'Part time', value: 'PartTime' },
    { label: 'Contract',  value: 'Contract' },
    { label: 'Internship', value: 'Internship' },
    { label: 'Temporary', value: 'Temporary' },
    { label: 'Other',     value: 'Other' },
  ];
  ngOnInit() {
  }
  constructor() {
    effect(() => {
      const aiResponse = this.store.aiResponse();
      if (aiResponse) {
        this.form.controls.title.setValue(aiResponse.title);
        this.form.controls.aboutRole.setValue(aiResponse.aboutRole);
        this.form.controls.location.setValue(aiResponse.location);
        this.setStringArray('responsibilities', aiResponse.responsibilities);
        this.setStringArray('qualifications', aiResponse.qualifications);
      }
    });
  }

  responsibilities(): FormArray<FormControl<string>> {
    return this.form.controls.responsibilities;
  }
  qualifications(): FormArray<FormControl<string>> {
    return this.form.controls.qualifications;
  }

  addResponsibility(initial = ''): void {
    this.responsibilities().push(new FormControl<string>(initial, { nonNullable: true }));
    this.setStringArray('responsibilities', this.responsibilities().value);
  }
  removeResponsibility(i: number): void {
    this.responsibilities().removeAt(i);
    this.setStringArray('responsibilities', this.responsibilities().value)
  }

  addQualification(initial = ''): void {
    this.qualifications().push(new FormControl<string>(initial, { nonNullable: true }));
    this.setStringArray('qualifications', this.qualifications().value);
  }
  removeQualification(i: number): void {
    this.qualifications().removeAt(i);
    this.setStringArray('qualifications', this.qualifications().value)
  }

  private setStringArray(
    key: 'responsibilities' | 'qualifications',
    values: string[]
  ) {
    const arr = this.fb.array(values.map(v => this.fb.control(v, { nonNullable: true })));
    this.form.setControl(key, arr);               // replaces the FormArray safely
    arr.markAsDirty();                            // optional UX
    arr.updateValueAndValidity({ emitEvent: true });
  }

  enhanceWithAI(type: string, i: number) {
    let text:string
    if (type=='responsibilities') {
      text= this.form.controls.responsibilities.value[i]

    }else{
      text = this.form.controls.qualifications.value[i]
    }
    console.log(text);
  }

  submit(){
    console.log(this.form.value);
  }
  saveDraft() {

  }
}
