import {Component, computed, inject, OnInit} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {InputText} from 'primeng/inputtext';
import {Textarea} from 'primeng/textarea';
import {Button, ButtonDirective} from 'primeng/button';
import {FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {JobType} from '../../../core/types/Dtos/CreateJobRequest';
import {Select} from 'primeng/select';
import {Tooltip} from 'primeng/tooltip';

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
    Tooltip
  ],
  templateUrl: './job-upsert.html',
  styleUrl: './job-upsert.css'
})
export class JobUpsert implements OnInit {
  store = inject(JobsStore)
  private fb = inject(FormBuilder)
  jobTypes: { label: string; value: JobType }[] = [
    { label: 'Full time', value: 'FullTime' },
    { label: 'Part time', value: 'PartTime' },
    { label: 'Contract',  value: 'Contract' },
    { label: 'Internship', value: 'Internship' },
    { label: 'Temporary', value: 'Temporary' },
    { label: 'Other',     value: 'Other' },
  ];

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    aboutRole: ['', [Validators.required, Validators.minLength(20)]],
    location: ['', Validators.required],
    jobType: this.fb.nonNullable.control<JobType>('FullTime', Validators.required),
    salaryRange: this.fb.control<string | null>(null),
    responsibilities: this.fb.array<FormControl<string>>([]),
    qualifications: this.fb.array<FormControl<string>>([]),
  });
  ngOnInit() {
    // this.setStringArray('responsibilities', [
    //   'Own core module architecture',
    //   'Implement CI-ready code with lint/tests',
    //   'Document tech decisions',
    // ]);
  }
  responsibilities(): FormArray<FormControl<string>> {
    return this.form.controls.responsibilities;
  }
  qualifications(): FormArray<FormControl<string>> {
    return this.form.controls.qualifications;
  }

  addResponsibility(initial = ''): void {
    this.responsibilities().push(new FormControl<string>(initial, { nonNullable: true }));
  }
  removeResponsibility(i: number): void {
    this.responsibilities().removeAt(i);
  }

  addQualification(initial = ''): void {
    this.qualifications().push(new FormControl<string>(initial, { nonNullable: true }));
  }
  removeQualification(i: number): void {
    this.qualifications().removeAt(i);
  }

  submit(){
    console.log(this.form.value);
  }

  company = computed(() => this.store.selectedCompany());

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
}
