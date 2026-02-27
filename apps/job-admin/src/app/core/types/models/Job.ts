export type Job = {
  uId: string;
  title: string;
  companyUId: string;
  companyName: string;
  location: string;
  jobType: string;
  aboutRole: string;
  salaryRange?: string;
  responsibilities: string[];
  qualifications: string[];
  createdAt: string;
  updatedAt?: string;
}
