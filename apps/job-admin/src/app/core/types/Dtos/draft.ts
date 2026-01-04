import {RoleLevel, Tone} from './JobGen';
import {JobType} from './CreateJobRequest';

export interface Draft {
  id: string;
  title: string;
  aboutRole: string;
  responsibilities: string[];
  qualifications: string[];
  notes: string;
  location: string;
  jobType: JobType;
  salaryRange?: string | null;
  metadata: {
    roleLevel: RoleLevel;
    tone: Tone
  }
}
