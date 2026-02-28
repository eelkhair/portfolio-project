export type JobType = 'Full-time' | 'Part-time' | 'Contract' | 'Remote' | 'Internship';
export type ExperienceLevel = 'Entry' | 'Mid' | 'Senior' | 'Lead' | 'Principal';

export interface Job {
  id: string;
  title: string;
  companyId: string;
  companyName: string;
  location: string;
  type: JobType;
  experienceLevel: ExperienceLevel;
  salary: string;
  description: string;
  responsibilities: string[];
  qualifications: string[];
  skills: string[];
  postedAt: Date;
  featured: boolean;
}
