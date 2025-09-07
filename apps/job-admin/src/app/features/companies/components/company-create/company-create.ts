import {Component, ElementRef, inject, viewChild} from '@angular/core';
import {Dialog} from 'primeng/dialog';
import {Button, ButtonDirective} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {CompanyStore} from '../../company.store';
import {Divider} from 'primeng/divider';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
import {Select} from 'primeng/select';
import {INDUSTRIES} from '../helpers/industries';
import {Company} from '../../../../core/types/models/Company';

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
  industries = INDUSTRIES;
  nameInput = viewChild<ElementRef<HTMLInputElement>>('name');
  form = new FormGroup({
    name: new FormControl<string>('', Validators.required),
    companyEmail: new FormControl('', [Validators.required, Validators.email]),
    companyWebsite: new FormControl(''),
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
      name: '',
      createdAt: '',
      updatedAt: '',
      uId: ''
    })
  }

  closeDialog() {
    this.store.showCompanyDialog.set(false);
    this.form.reset();
  }
}
