export type JobType = 'fullTime' | 'partTime' | 'contract' | 'internship';

export interface Job {
  id: string;
  title: string;
  companyUId: string;
  companyName: string;
  location: string;
  jobType: JobType;
  aboutRole: string;
  salaryRange: string | null;
  responsibilities: string[];
  qualifications: string[];
  createdAt: string;
  updatedAt: string | null;
}

export interface MatchingJob {
  jobId: string;
  title: string;
  aboutRole: string | null;
  salaryRange: string | null;
  similarity: number;
}
