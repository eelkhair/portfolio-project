import { WorkHistoryDto, EducationDto, CertificationDto } from './application.type';

export interface ResumeData {
  fullName: string;
  email: string;
  phone: string;
  linkedin: string;
  portfolio: string;
  experience: string;
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
  experience?: string;
  skills: string[];
  preferredLocation?: string;
  preferredJobType?: string;
}

export interface UserProfileRequest {
  phone?: string;
  linkedin?: string;
  portfolio?: string;
  experience?: string;
  skills?: string[];
  preferredLocation?: string;
  preferredJobType?: string;
}

export interface ResumeResponse {
  id: string;
  originalFileName: string;
  contentType?: string;
  fileSize?: number;
  hasParsedContent: boolean;
  createdAt: string;
}

export type ParseStatus = 'idle' | 'uploading' | 'parsing' | 'parsed' | 'error';
