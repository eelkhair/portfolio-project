// models/job-gen.models.ts

export type RoleLevel = 'junior' | 'mid' | 'senior';
export type Tone = 'neutral' | 'concise' | 'friendly';

export interface JobGenRequest {
  brief: string;
  roleLevel: RoleLevel;      // default 'mid' in UI
  tone: Tone;                // default 'neutral' in UI
  maxBullets: number;        // 3..8

  // optional context
  companyName?: string;
  teamName?: string;
  location?: string;         // "City, ST" | "Remote" | "Hybrid"
  titleSeed?: string;
  techStackCSV?: string;
  mustHavesCSV?: string;
  niceToHavesCSV?: string;
  benefits?: string;
}

export interface JobGenResponse {
  title: string;
  aboutRole: string;
  responsibilities: string[];
  qualifications: string[];
  id?: string;
  notes: string;
  location: string;
  jobType?: string;
  salaryRange?:string;
  metadata: {
    roleLevel: RoleLevel;
    tone: Tone;
  };
}
