
export type JobType = 'fullTime' | 'partTime' | 'contract' | 'internship' | 'temporary' | 'other';

export const JOB_TYPE_LABELS: Record<JobType, string> = {
  fullTime: 'Full Time',
  partTime: 'Part Time',
  contract: 'Contract',
  internship: 'Internship',
  temporary: 'Temporary',
  other: 'Other',
};

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
