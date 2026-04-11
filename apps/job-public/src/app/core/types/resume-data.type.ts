import { WorkHistoryDto, EducationDto, CertificationDto } from './application.type';

export interface ProjectDto {
  name: string;
  description?: string;
  technologies: string[];
  url?: string;
}

export interface ResumeData {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  linkedin: string;
  portfolio: string;
  skills: string[];
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
  summary?: string;
  projects?: ProjectDto[];
}

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  linkedin?: string;
  portfolio?: string;
  about?: string;
  skills: string[];
  preferredLocation?: string;
  preferredJobType?: string;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
  summary?: string;
  projects?: ProjectDto[];
}

export interface UserProfileRequest {
  phone?: string;
  linkedin?: string;
  portfolio?: string;
  about?: string;
  skills?: string[];
  preferredLocation?: string;
  preferredJobType?: string;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
  projects?: ProjectDto[];
}

export interface ResumeResponse {
  id: string;
  originalFileName: string;
  contentType?: string;
  fileSize?: number;
  hasParsedContent: boolean;
  parseStatus: 'Pending' | 'Processing' | 'PartiallyParsed' | 'Parsed' | 'Failed';
  parseRetryCount: number;
  isDefault: boolean;
  createdAt: string;
  parsedContent?: ResumeData;
}

export type ParseStatus = 'idle' | 'uploading' | 'parsing' | 'partial' | 'complete' | 'ready' | 'parsed' | 'error' | 'retrying';

export type ResumeSection = 'contact' | 'skills' | 'workHistory' | 'education' | 'certifications' | 'projects';

export type SectionStatus = 'pending' | 'parsing' | 'done' | 'failed';

export const ALL_RESUME_SECTIONS: ResumeSection[] = ['contact', 'skills', 'workHistory', 'education', 'certifications', 'projects'];

export const SECTION_LABELS: Record<ResumeSection, string> = {
  contact: 'Contact Info',
  skills: 'Summary & Skills',
  workHistory: 'Work History',
  education: 'Education',
  certifications: 'Certifications',
  projects: 'Projects',
};

export interface ResumeParsedMsg {
  resumeId: string;
  currentPage?: string;
  traceParent?: string;
  traceState?: string;
}

export interface ResumeEmbeddedMsg {
  resumeId: string;
  traceParent?: string;
  traceState?: string;
}

export interface ResumeParseFailedMsg {
  resumeId: string;
  currentPage?: string;
  status: 'retrying' | 'failed';
  attempt: number;
  maxAttempts: number;
  traceParent?: string;
  traceState?: string;
}

export interface ResumeSectionParsedMsg {
  resumeId: string;
  section: ResumeSection;
  traceParent?: string;
  traceState?: string;
}

export interface ResumeSectionFailedMsg {
  resumeId: string;
  section: ResumeSection;
  traceParent?: string;
  traceState?: string;
}

export interface ResumeAllSectionsCompletedMsg {
  resumeId: string;
  traceParent?: string;
  traceState?: string;
}
