import {Component, computed, effect, inject,  OnDestroy, OnInit} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {CompanySelection} from '../../../shared/companies/company-selection/company-selection';
import {InputText} from 'primeng/inputtext';
import {Textarea} from 'primeng/textarea';
import {Button, ButtonDirective} from 'primeng/button';
import {FormArray, FormBuilder, FormControl, ReactiveFormsModule, Validators} from '@angular/forms';
import {CreateJobDto, JOB_TYPE_LABELS, JobType} from '../../../core/types/Dtos/CreateJobRequest';
import {Select} from 'primeng/select';
import {Tooltip} from 'primeng/tooltip';
import {JobGenerate} from '../job-generate/job-generate';
import {Draft} from '../../../core/types/Dtos/draft';
import {JobAIEnhancerStore} from '../ai-enhancer.store';
import {DialogService, DynamicDialogRef} from 'primeng/dynamicdialog';
import {EnhancementRequest} from '../../../core/types/Dtos/EnhancementDto';
import {JobEnhancer} from '../job-enhancer/job-enhancer';
import {ActivatedRoute, Router} from '@angular/router';
import {JobPublishConfirm} from '../job-publish-confirm/job-publish-confirm';
import {toSignal} from '@angular/core/rxjs-interop';
import {distinctUntilChanged, map} from 'rxjs';

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
export class JobUpsert implements OnInit, OnDestroy{
  ref?: DynamicDialogRef;
  confirmRef?: DynamicDialogRef;
  route = inject(ActivatedRoute);
  router= inject(Router);
  dialogService = inject(DialogService);
  store = inject(JobsStore)
  readonly draftId = toSignal(
    this.route.paramMap.pipe(
      map(p => p.get('draftId')),
      distinctUntilChanged()
    ),
    { initialValue: null }
  );
  enhanceStore = inject(JobAIEnhancerStore)
  private fb = inject(FormBuilder)
  company = computed(() => this.store.selectedCompany());

  form = this.fb.nonNullable.group({
    title: ['', Validators.required],
    aboutRole: ['', [Validators.required, Validators.minLength(20)]],
    location: ['', Validators.required],
    jobType: this.fb.nonNullable.control<JobType>('fullTime', Validators.required),
    salaryRange: this.fb.control<string | null>(null),
    responsibilities: this.fb.array<FormControl<string>>([]),
    qualifications: this.fb.array<FormControl<string>>([]),
  });

  jobTypes = Object.entries(JOB_TYPE_LABELS).map(([value, label]) => ({ label, value: value as JobType }));
  ngOnInit() {
    const draftId = this.draftId()

    if(draftId) {
      if (!this.store.selectedCompany()) {
        void this.router.navigate(["jobs", "drafts"])
      }else{
        this.store.populateDraft(draftId);
      }
    }
  }
  ngOnDestroy() {
    this.form.reset();
    this.store.aiResponse.set(undefined);
  }
  constructor() {
    effect(() => {
      const aiResponse = this.store.aiResponse();
      if (aiResponse) {
        this.form.controls.title.setValue(aiResponse.title);
        this.form.controls.aboutRole.setValue(aiResponse.aboutRole);
        this.form.controls.location.setValue(aiResponse.location);
        if (aiResponse.jobType) {
          this.form.controls.jobType.setValue(aiResponse.jobType as JobType);
        }
        this.form.controls.salaryRange.setValue(aiResponse.salaryRange??"");
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
  submit() {
    if (this.form.invalid) return;
    this.confirmRef = this.dialogService.open(JobPublishConfirm, {
      header: "Confirm Publish",
      width:'25rem',
      modal:true,closable:true, closeOnEscape: true,
      data:{draftId: this.draftId()}
    });
    this.confirmRef.onClose.subscribe(result => {
      if(result === undefined) return;
      const model = {
        ...this.form.value,
        draftId: this.store.aiResponse()?.id,
        companyUId: this.store.selectedCompany()?.uId!,
        deleteDraft: result
      } as CreateJobDto
      this.store.createJob(model).subscribe({
        next: () => {
          this.store.notificationService.success("Success","The job was published successfully.");
          void this.router.navigate(["jobs"]);
        }
      })
    });
  }
  saveDraft() {
    const payload: Draft = {
      aboutRole: this.form.controls.aboutRole.value,
      id: this.store.aiResponse()?.id ??'',
      jobType: this.form.controls.jobType.value,
      location: this.form.controls.location.value,
      metadata: {roleLevel: this.store.aiResponse()?.metadata?.roleLevel?? 'mid', tone: this.store.aiResponse()?.metadata?.tone??'neutral'},
      notes: this.store.aiResponse()?.notes ?? '',
      qualifications: this.form.controls.qualifications.value,
      responsibilities: this.form.controls.responsibilities.value,
      salaryRange: this.form.controls.salaryRange.value ??'',
      title: this.form.controls.title.value ??''

    }
    this.store.saveDraft(payload).subscribe({
      next: ()=>{
        this.store.notificationService.success('Success', 'The draft was saved successfully!')
      }
    })
  }

  enhanceAboutMe() {
    const model = this.enhanceStore.buildModel('aboutRole', this.form.controls.aboutRole.value,{
      title: this.form.controls.title.value,
      qualifications: this.form.controls.qualifications.value .filter(c=> c.length >= 3),
      responsibilities: this.form.controls.responsibilities.value .filter(c=> c.length >= 3)
    })
    if(model != undefined){
      this.showDialog(model);
    }
  }
  enhanceResponsibility(i: number) {
    const text= this.form.controls.responsibilities.value[i];
    const model = this.enhanceStore.buildModel('responsibilities', text,{
      title: this.form.controls.title.value,
      qualifications: this.form.controls.qualifications.value .filter(c=> c.length >= 3),
      aboutRole: this.form.controls.aboutRole.value
    });
    if(model != undefined){
      this.showDialog(model,i);
    }
  }

  enhanceQualification(i: number) {
    const text= this.form.controls.qualifications.value[i];

    const model = this.enhanceStore.buildModel('qualifications', text,{
      title: this.form.controls.title.value,
      responsibilities: this.form.controls.responsibilities.value
        .filter(c=> c.length >= 3)
      ,
      aboutRole: this.form.controls.aboutRole.value
    });
    if(model != undefined){
      this.showDialog(model, i);
    }

  }
  showDialog(model: EnhancementRequest, selectedIndex: number|undefined = undefined) {
    if (!model.value){return;}
    model.context["CompanyName"] = this.company()?.name;
    let header: string;
    switch (model.field){
      case 'aboutRole':
        header = 'Enhance About Role';
        break;
      case 'responsibilities':
        header = 'Enhance Responsibility';
        break;
      case 'qualifications':
        header = 'Enhance Qualification';
        break;
      default:
        header = 'Enhancement';
    }

    this.ref = this.dialogService.open(JobEnhancer, {
      header: header,
      width:'60rem',
      modal:true,
      closable:true,
      closeOnEscape: true,
      data: {model: model},
    });

    this.ref.onClose.subscribe(result => {
     if(!result ){ return; }
      if(selectedIndex != undefined){
        if(model.field== 'responsibilities'){
          let current = this.form.controls.responsibilities.value;
          current[selectedIndex] = result;
          this.form.controls.responsibilities.setValue(current);
        }else{
          let current = this.form.controls.qualifications.value;
          current[selectedIndex] = result;
          this.form.controls.qualifications.setValue(current);
        }
      }else{
        this.form.controls.aboutRole.setValue(result);
      }
    })
  }
}
