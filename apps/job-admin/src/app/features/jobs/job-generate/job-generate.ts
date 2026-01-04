import {Component, inject, input, OnInit, signal, viewChild} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {Dialog} from 'primeng/dialog';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {Step, StepList, StepPanel, StepPanels, Stepper} from 'primeng/stepper';
import {Textarea} from 'primeng/textarea';
import {Select} from 'primeng/select';
import {AutoComplete} from 'primeng/autocomplete';
import {FormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import {JobGenRequest, RoleLevel, Tone} from '../../../core/types/Dtos/JobGen';
import {locationValidator, normalizeLocation} from './utils/jobGenLocation.validator';
import {ProgressSpinner} from 'primeng/progressspinner';

@Component({
  selector: 'app-job-generate',
  imports: [
    Dialog,
    Button,
    InputText,
    Stepper,
    StepList,
    Step,
    StepPanels,
    StepPanel,
    Textarea,
    Select,
    AutoComplete,
    ReactiveFormsModule,
    ProgressSpinner,
  ],
  templateUrl: './job-generate.html',
  styleUrl: './job-generate.css'
})
export class JobGenerate implements OnInit {

  store = inject(JobsStore);
  private stepper = viewChild<Stepper>('stepper');
  private fb = inject(FormBuilder)
  companyName=input.required<string>()
  errors = signal<string[]>([])
  validSteppers: Record<number, boolean> = {1:false, 2:false, 3:false, 4:true};
  isFinalStep = signal(false)
  success=signal(false)

  form = this.fb.nonNullable.group({
    basics:this.fb.group({
      companyName: ['',Validators.required],
      teamName : ['', Validators.required],
      titleSeed: ['',Validators.required],
      location: ['', locationValidator()],
    }),
    scope:this.fb.group({
      brief:['',[Validators.required, Validators.minLength(10)]],
      roleLevel:['Mid',Validators.required],
    }),
    qualifications:this.fb.group({
      mustHaves: this.fb.control<string[]>([], { nonNullable: true }),
      niceToHaves: this.fb.control<string[]>([], { nonNullable: true }),
      techStack: this.fb.control<string[]>([], { nonNullable: true })
    }),
    style:this.fb.group({
      maxBullets:[6,[Validators.required, Validators.min(3), Validators.max(8)]],
      tone:['Neutral', Validators.required],
      benefits:[''],
    }),
  })

  ngOnInit() {
    this.validateSteps();
    this.setDefaults()
  }
  protected setDefaults(){
    this.form.controls.style.controls.maxBullets.setValue(6);
    this.form.controls.style.controls.tone.setValue('Neutral');
    this.form.controls.scope.controls.roleLevel.setValue('Mid');
    this.form.controls.basics.controls.companyName.setValue(this.companyName());
    this.form.controls.basics.statusChanges.subscribe(()=> this.validateStep(1))
    this.form.controls.scope.statusChanges.subscribe(()=> this.validateStep(2) )
    this.form.controls.qualifications.statusChanges.subscribe(()=> this.validateStep(3))
    this.form.controls.style.statusChanges.subscribe(()=> this.validateStep(4))
  }
  submit() {

    if(this.form.invalid) {
      this.success.set(false)
      const errs = this.store.getAllErrors(this.form);
      this.errors.set(this.store.buildErrors(errs))
      return;
    }
    this.isFinalStep.set(true)
    this.errors.set([])
    for (let i = 0; i<5;i++){
      if(this.validSteppers[i]){
        this.validSteppers[i] = false;
      }

    }
    const payload: JobGenRequest = {
      ...this.form.controls.basics.value,
      ...this.form.controls.scope.value,
      ...this.form.controls.style.value,
      benefits:this.form.controls.style.controls.benefits.value??'',
      location: normalizeLocation(this.form.controls.basics.value.location),
      tone:this.form.controls.style.controls.tone.value?.toLowerCase() as Tone?? '',
      roleLevel:this.form.controls.scope.controls.roleLevel.value?.toLowerCase() as RoleLevel ?? '',
      techStackCSV: this.form.controls.qualifications.controls.techStack.value.join(', ')??'',
      mustHavesCSV: this.form.controls.qualifications.controls.mustHaves.value.join(', ')??'',
      niceToHavesCSV: this.form.controls.qualifications.controls.niceToHaves.value.join(', ')??''
    } as JobGenRequest;
    this.store.generateDraft(payload).subscribe({
      next: ()=>{
        this.isFinalStep.set(false)
        this.form.reset();
        this.success.set(true)
        this.store.notificationService.success("Success", "The draft was generated successfully!")
        this.setDefaults()
      }, error: () => {
        this.store.notificationService.error("Error","Failed to generate draft. \nPlease try again later");
        this.validateSteps()
        this.stepper()?.value.set(4)
        this.isFinalStep.set(false)
        this.success.set(false)
      }
    }
   )
  }

  validateStep(i: number | undefined) {
    let errs: Array<{ path: string; validator: string; details: any }>;

    switch(i){
      case 1:
        if(this.form.controls.basics.dirty){
          errs = this.store.getAllErrors(this.form.controls.basics);
          this.errors.set(this.store.buildErrors(errs))
        }

        this.validateSteps()
        break;
      case 2:
        if(this.form.controls.scope.dirty) {
          errs = this.store.getAllErrors(this.form.controls.scope);
          this.errors.set(this.store.buildErrors(errs))
        }
        this.validateSteps()
        break;
      case 3:
        if (this.form.controls.qualifications.dirty){
          errs = this.store.getAllErrors(this.form.controls.qualifications);
          this.errors.set(this.store.buildErrors(errs))
        }
        this.validateSteps()
        break;
      case 4:
        if(this.form.controls.style.dirty){
          errs = this.store.getAllErrors(this.form.controls.style);
          this.errors.set(this.store.buildErrors(errs))
        }

        this.validateSteps()
        break;
    }
  }
  validateSteps(){
    this.validSteppers[1] = this.form.controls.basics.valid;
    this.validSteppers[2] = this.form.controls.scope.valid
      && this.validSteppers[1]
    this.validSteppers[3] = this.form.controls.qualifications.valid
      && this.validSteppers[2]
    this.validSteppers[4] = this.form.controls.style.valid && this.validSteppers[3];
    this.isFinalStep.set(false)
  }
}
