
export type JobType = 'fullTime' | 'partTime' | 'contract' | 'internship' | 'temporary' | 'other';

export interface CreateJobDto {
  title: string;
  companyUId: string;
  location: string;
  aboutRole: string;
  jobType: JobType;
  salaryRange?: string | null;
  responsibilities: string[];
  qualifications: string[];
  draftId?: string;
  deleteDraft: boolean;
}
