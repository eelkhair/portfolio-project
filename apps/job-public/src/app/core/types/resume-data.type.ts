import { WorkHistoryDto, EducationDto, CertificationDto } from './application.type';

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
}

export interface UserProfile {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  linkedin?: string;
  portfolio?: string;
  skills: string[];
  preferredLocation?: string;
  preferredJobType?: string;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
}

export interface UserProfileRequest {
  phone?: string;
  linkedin?: string;
  portfolio?: string;
  skills?: string[];
  preferredLocation?: string;
  preferredJobType?: string;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
}

export interface ResumeResponse {
  id: string;
  originalFileName: string;
  contentType?: string;
  fileSize?: number;
  hasParsedContent: boolean;
  parseStatus: 'Pending' | 'Processing' | 'Parsed' | 'Failed';
  parseRetryCount: number;
  isDefault: boolean;
  createdAt: string;
  parsedContent?: ResumeData;
}

export type ParseStatus = 'idle' | 'uploading' | 'parsing' | 'ready' | 'parsed' | 'error' | 'retrying';

export interface ResumeParsedMsg {
  resumeId: string;
  currentPage?: string;
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
