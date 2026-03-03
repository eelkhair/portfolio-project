export interface PersonalInfoDto {
  firstName: string;
  lastName: string;
  email: string;
  phone?: string;
  linkedin?: string;
  portfolio?: string;
}

export interface WorkHistoryDto {
  company: string;
  jobTitle: string;
  startDate: string;
  endDate?: string;
  description?: string;
  isCurrent: boolean;
}

export interface EducationDto {
  institution: string;
  degree: string;
  fieldOfStudy?: string;
  startDate: string;
  endDate?: string;
}

export interface CertificationDto {
  name: string;
  issuingOrganization?: string;
  issueDate?: string;
  expirationDate?: string;
  credentialId?: string;
}

export interface SubmitApplicationRequest {
  jobId: string;
  resumeId?: string;
  coverLetter?: string;
  personalInfo?: PersonalInfoDto;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
  skills?: string[];
}

export interface ApplicationResponse {
  id: string;
  jobId: string;
  jobTitle: string;
  companyName: string;
  status: string;
  createdAt: string;
}

export type ApplicationStatus = 'idle' | 'submitting' | 'submitted' | 'error';
