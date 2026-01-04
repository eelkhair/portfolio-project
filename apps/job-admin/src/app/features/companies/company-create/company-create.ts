import {Component, ElementRef, inject, signal, viewChild} from '@angular/core';
import {Dialog} from 'primeng/dialog';
import {Button, ButtonDirective} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {CompanyStore} from '../company.store';
import {Divider} from 'primeng/divider';
import {FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {Select} from 'primeng/select';
import {ApiError} from '../../../core/types/Dtos/ApiResponse';
@Component({
  selector: 'app-company-create',
  imports: [
    Dialog,
    Button,
    InputText,
    Divider,
    ReactiveFormsModule,
    Select,
    ButtonDirective,
  ],
  templateUrl: './company-create.html',
  styleUrl: './company-create.css'
})
export class CompanyCreate {
  store= inject(CompanyStore);
  errors= signal<Record<string, string[]>|undefined>(undefined);
  industries = this.store.industries
  nameInput = viewChild<ElementRef<HTMLInputElement>>('name');
  form = new FormGroup({
    name: new FormControl<string>('', Validators.required),
    companyEmail: new FormControl('', [Validators.required, Validators.email]),
    companyWebsite: new FormControl(undefined),
    industry: new FormControl('', Validators.required),
    adminFirstName: new FormControl('', Validators.required),
    adminLastName: new FormControl('', Validators.required),
    adminEmail: new FormControl('', Validators.required),
  })
  focusName() {
    setTimeout(() => {
      this.nameInput()?.nativeElement.focus()
    },50)

  }

  saveForm() {
    if(this.form.invalid){
      this.form.markAllAsTouched();
      return;
    }
    this.store.createCompany({
      name: this.form.controls.name.value!,
      companyEmail: this.form.controls.companyEmail.value!,
      industryUId: this.form.controls.industry.value!,
      adminFirstName: this.form.controls.adminFirstName.value!,
      adminLastName: this.form.controls.adminLastName.value!,
      adminEmail: this.form.controls.adminEmail.value!,
      companyWebsite: this.form.controls.companyWebsite.value!,
    }).subscribe({
      next:company => {
        this.store.notificationService.info("Provisioning!",`The company '${company.data?.name}' is being provisioned!`)
      },
      error: (err: any) => {
        const errors = err.error?.exceptions as ApiError|null|undefined;
        if(errors){
          this.errors.set(errors.errors)
        }

      }
    })
  }

  closeDialog() {
    this.store.showCreateCompanyDialog.set(false);
    this.form.reset();
  }
}
