
export type JobType = 'FullTime' | 'PartTime' | 'Contract' | 'Internship' | 'Temporary' | 'Other';

export interface CreateJobDto {
  title: string;
  companyUId: string;
  location: string;
  jobType: JobType;
  aboutRole: string;
  salaryRange?: string | null;
  responsibilities: string[];
  qualifications: string[];
}
