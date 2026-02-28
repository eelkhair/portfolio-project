import {Component, effect, inject, OnInit, input, signal} from '@angular/core';
import {DatePipe} from '@angular/common';
import {CompanyStore} from '../company.store';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {InputText} from 'primeng/inputtext';
import {Textarea} from 'primeng/textarea';
import {Select} from 'primeng/select';
import {ButtonDirective} from 'primeng/button';
import {Divider} from 'primeng/divider';
import {ApiError} from '../../../core/types/Dtos/ApiResponse';

@Component({
  selector: 'app-details',
  imports: [
    ReactiveFormsModule,
    InputText,
    Textarea,
    Select,
    ButtonDirective,
    Divider,
    DatePipe,
  ],
  templateUrl: './company-details.html',
  styleUrl: './company-details.css'
})
export class CompanyDetails implements OnInit {
  store = inject(CompanyStore);
  id = input.required<string>();
  errors = signal<Record<string, string[]> | undefined>(undefined);
  industries = this.store.industries;

  form = new FormGroup({
    name: new FormControl<string>('', Validators.required),
    companyEmail: new FormControl('', [Validators.required, Validators.email]),
    companyWebsite: new FormControl(''),
    phone: new FormControl(''),
    description: new FormControl(''),
    about: new FormControl(''),
    eeo: new FormControl(''),
    founded: new FormControl(''),
    size: new FormControl(''),
    logo: new FormControl(''),
    industryUId: new FormControl('', Validators.required),
  });

  constructor() {
    effect(() => {
      const company = this.store.selectedCompany();
      if (company) {
        this.form.patchValue({
          name: company.name ?? '',
          companyEmail: company.email ?? '',
          companyWebsite: company.website ?? '',
          phone: company.phone ?? '',
          description: company.description ?? '',
          about: company.about ?? '',
          eeo: company.eeo ?? '',
          founded: company.founded ? String(company.founded).substring(0, 10) : '',
          size: company.size ?? '',
          logo: company.logo ?? '',
          industryUId: company.industryUId ?? '',
        });
      }
    });
  }

  ngOnInit(): void {
    this.store.loadCompany(this.id());
  }

  saveForm() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.errors.set(undefined);

    this.store.updateCompany(this.id(), {
      name: this.form.controls.name.value!,
      companyEmail: this.form.controls.companyEmail.value!,
      companyWebsite: this.form.controls.companyWebsite.value || undefined,
      phone: this.form.controls.phone.value || undefined,
      description: this.form.controls.description.value || undefined,
      about: this.form.controls.about.value || undefined,
      eeo: this.form.controls.eeo.value || undefined,
      founded: this.form.controls.founded.value || undefined,
      size: this.form.controls.size.value || undefined,
      logo: this.form.controls.logo.value || undefined,
      industryUId: this.form.controls.industryUId.value!,
    }).subscribe({
      error: (err: any) => {
        const errors = err.error?.exceptions as ApiError | null | undefined;
        if (errors) {
          this.errors.set(errors.errors);
        }
      }
    });
  }
}
