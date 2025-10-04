import {Component, effect, inject, input, OnInit} from '@angular/core';
import {JobsStore} from '../jobs.store';
import {Dialog} from 'primeng/dialog';
import {Button} from 'primeng/button';
import {InputText} from 'primeng/inputtext';
import {Step, StepList, StepPanel, StepPanels, Stepper} from 'primeng/stepper';
import {Textarea} from 'primeng/textarea';
import {Select} from 'primeng/select';
import {Tooltip} from 'primeng/tooltip';
import {AutoComplete} from 'primeng/autocomplete';
import {FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators} from '@angular/forms';
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
    Tooltip,
    AutoComplete,
    ReactiveFormsModule
  ],
  templateUrl: './job-generate.html',
  styleUrl: './job-generate.css'
})
export class JobGenerate implements OnInit {
  store = inject(JobsStore);
  private fb = inject(FormBuilder)
  skillSuggestions: string[] = [];
  companyName=input<string>('test')

  form = this.fb.nonNullable.group({
    brief:['',Validators.required, Validators.minLength(10)],
    companyName: ['',Validators.required],
    teamName : [''],
    titleSeed: ['',Validators.required],
    location: [''],
    mustHaves: this.fb.control<string[]>([], { nonNullable: true }),
    niceToHaves: this.fb.control<string[]>([], { nonNullable: true })
  })
  ngOnInit() {
    this.form.controls.companyName.setValue(this.companyName())
  }
  private allSkills = [
    '.NET','REST','SQL','Kafka','Terraform','Azure','Kubernetes','Docker','PostgreSQL','C#','Java','Go'
  ];

  onComplete(e: { query: string }, current: string[]) {
    const q = (e.query || '').trim().toLowerCase();

    const base = this.allSkills
      .filter(s => s.toLowerCase().includes(q))
      .filter(s => !current.includes(s))
      .slice(0, 9); // leave room for the "add query" item

    const includeQuery = q.length > 0 &&
      !current.map(x => x.toLowerCase()).includes(q) &&
      !base.map(x => x.toLowerCase()).includes(q);

    this.skillSuggestions = includeQuery ? [e.query, ...base] : base;
  }

// When sending to your API later:
 // const mustHavesCSV = this.mustHaves.join(', ');
  submit(e: SubmitEvent) {
    e.preventDefault();
    console.log(this.form.value, this.form.valid);
  }
}
