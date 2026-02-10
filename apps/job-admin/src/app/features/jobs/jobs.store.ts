import {inject, Injectable, signal} from '@angular/core';
import {JobService} from '../../core/services/job.service';
import {CompanySelectionStore} from '../../shared/companies/company-selection/company-selection.store';
import {Job} from '../../core/types/models/Job';
import {cities} from './job-generate/us-cities';
import {JobGenRequest, JobGenResponse} from '../../core/types/Dtos/JobGen';
import {tap} from 'rxjs/operators';
import {NotificationService} from '../../core/services/notification.service';
import {Draft} from '../../core/types/Dtos/draft';
import {CreateJobDto} from '../../core/types/Dtos/CreateJobRequest';

@Injectable({ providedIn: 'root' })
export class JobsStore {
  companySelectionStore = inject(CompanySelectionStore);
  selectedCompany = this.companySelectionStore.selectedCompany;
  private jobService = inject(JobService);
  jobs = signal<Job[]>([])
  drafts= signal<Draft[]>([])
  showGenerate = signal(false);
  aiResponse = signal<JobGenResponse|undefined>(undefined)
  skillSuggestions: string[] = [];
  techStackSuggestions: string[] = [];
  citySuggestions: string[] = [];
  notificationService = inject(NotificationService);


  loadJobs(){
    const selectedCompany = this.selectedCompany();
    if(selectedCompany){
      this.jobService.list(selectedCompany.uId).subscribe({
        next: response => {
          this.jobs.set(response.data!)
        }
      })
    }
    return undefined;
  }

  generateDraft(payload: JobGenRequest) {
    return this.jobService.generateDraft(this.selectedCompany()?.uId!, payload).pipe(tap(job => {
      this.aiResponse.set(job.data)
    }));
  }
  saveDraft(payload: Draft) {
    const companyId = this.selectedCompany()?.uId!;
    return this.jobService.saveDraft(companyId, payload).pipe(tap(job => {
      let draft: JobGenResponse;
      draft = {
        aboutRole: job.data?.aboutRole??'',
        id: job.data?.id,
        location: job.data?.location??'',
        metadata: {roleLevel: payload.metadata?.roleLevel??'mid' , tone: payload.metadata?.tone??'neutral'},
        notes: job.data?.notes??'',
        qualifications: job.data?.qualifications??[],
        responsibilities: job.data?.responsibilities?? [],
        title: job.data?.title??'',
        jobType: payload.jobType??'',
        salaryRange:payload.salaryRange??''
      };
      this.aiResponse.set(draft);

      const saved: Draft = {
        ...payload,
        id: job.data?.id ?? payload.id,
      };
      this.drafts.update(list => {
        const idx = list.findIndex(d => d.id === saved.id);
        return idx >= 0
          ? list.map((d, i) => i === idx ? saved : d)
          : [saved, ...list];
      });
    }));
  }
  private allSkills = [
    '.NET','REST','SQL','Kafka','Terraform','Azure','Kubernetes','Docker','PostgreSQL','C#','Java','Go'
  ];

  private techStackArray = [
    '.NET 8', 'C#', 'ASP.NET Core', 'Entity Framework Core', 'SQL Server', 'Cosmos DB',
    'Angular 17', 'TypeScript', 'TailwindCSS', 'PrimeNG',
    'Next.js', 'React', 'Node.js', 'Fastify',
    'Docker', 'Dapr', 'RabbitMQ', 'Redis',
    'Azure App Service', 'Azure Functions', 'Azure Container Apps', 'Azure DevOps',
    'OpenAI API', 'Azure OpenAI', 'Python', 'FastEndpoints', 'Mapster'
  ]

  onTechStackComplete(e: { query: string; }, value: string[]) {
    const q = (e.query || '').trim().toLowerCase();

    const base = this.techStackArray
      .filter(s => s.toLowerCase().includes(q))
      .filter(s => !value.includes(s))
      .slice(0, 9); // leave room for the "add query" item

    const includeQuery = q.length > 0 &&
      !value.map(x => x.toLowerCase()).includes(q) &&
      !base.map(x => x.toLowerCase()).includes(q);

    this.techStackSuggestions = includeQuery ? [e.query, ...base] : base;
  }
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
  getAllErrors(control: import('@angular/forms').AbstractControl, path: string = ''): Array<{
    path: string; validator: string; details: any;
  }> {
    const out: Array<{ path: string; validator: string; details: any }> = [];

    if (control.errors) {
      Object.entries(control.errors).forEach(([validator, details]) => {
        out.push({ path: path || '(root)', validator, details });
      });
    }

    const anyControl = control as any;

    // FormGroup
    if (anyControl.controls && !(anyControl.length >= 0)) {
      Object.keys(anyControl.controls).forEach(key => {
        out.push(...this.getAllErrors(anyControl.controls[key], path ? `${path}.${key}` : key));
      });
    }

    // FormArray
    if (Array.isArray(anyControl.controls)) {
      anyControl.controls.forEach((c: any, i: number) => {
        out.push(...this.getAllErrors(c, `${path}[${i}]`));
      });
    }

    return out;
  }

  onCompleteCity(e: { query: string }) {
    const q = (e.query || '').trim().toLowerCase();
    if (!q) { this.citySuggestions = []; return; }

    // prefix/contains match; de-dupe already done at build time
    this.citySuggestions = cities
      .filter(x => x.toLowerCase().includes(q))
      .slice(0, 10);
  }
  buildErrors(errs: { path: string; validator: string; details: any; }[]): string[] {
    // [{ path, validator, details }]
    const seen = new Set<string>();

    const label = (path: string) =>
      path
        .replace(/([a-z])([A-Z])/g, '$1 $2')   // split camelCase
        .replace(/^\w/, c => c.toUpperCase()); // capitalise first

    const msg = (e: any) => {
      switch (e.validator) {
        case 'required':   return `${label(e.path)} is required`;
        case 'minlength':  return `${label(e.path)} must be at least ${e.details.requiredLength} characters`;
        case 'maxlength':  return `${label(e.path)} must be at most ${e.details.requiredLength} characters`;
        case 'min':        return `${label(e.path)} must be ≥ ${e.details.min}`;
        case 'max':        return `${label(e.path)} must be ≤ ${e.details.max}`;
        case 'email':      return `${label(e.path)} must be a valid email`;
        case 'pattern':    return `${label(e.path)} has an invalid format`;
        case 'arrayMin':   return `${label(e.path)} must have at least ${e.details.min} item(s)`;
        case 'arrayMax':   return `${label(e.path)} must have at most ${e.details.max} item(s)`;
        case 'distinct':   return `${label(e.path)} contains duplicate values`;
        case 'enum':       return `${label(e.path)} must be one of: ${e.details.allowed.join(', ')}`;
        case 'locationFormat': return `${label(e.path)} must be "City, ST", "Remote", "Hybrid", or empty`;
        default:           return `${label(e.path)} is invalid`;
      }
    };

    return errs.map(msg).filter(m => {
      if (seen.has(m)) return false;
      seen.add(m);
      return true;
    });
  }


  loadDrafts(companyId: string) {
    return this.jobService.loadDrafts(companyId).pipe(tap(drafts => {
      if(drafts?.data) {
        this.drafts.set(drafts?.data);
      }
    }));
  }

  populateDraft(id: string) {
    const drafts = this.drafts()
    let draft: Draft|undefined;
    if(drafts){
      draft =drafts.find(x => x.id === id);
    }
    if(!draft) return

    const response = draft as JobGenResponse;
    this.aiResponse.set(response)

  }

  createJob(model: CreateJobDto) {
    return this.jobService.createJob(model)
  }
}
