export type ApplicationStatus = 'Submitted' | 'UnderReview' | 'Shortlisted' | 'Rejected' | 'Accepted';

export interface ApplicationListItem {
  id: string;
  applicantName: string;
  applicantEmail: string;
  jobId: string;
  jobTitle: string;
  companyName: string;
  status: ApplicationStatus;
  createdAt: string;
  updatedAt?: string;
}

export interface ApplicationDetail extends ApplicationListItem {
  coverLetter?: string;
  resumeId?: string;
  personalInfo?: PersonalInfoDto;
  workHistory?: WorkHistoryDto[];
  education?: EducationDto[];
  certifications?: CertificationDto[];
  skills?: string[];
  projects?: ProjectDto[];
}

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

export interface ProjectDto {
  name: string;
  description?: string;
  technologies?: string[];
  url?: string;
}

export interface PaginatedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface PipelineStage {
  name: string;
  status: ApplicationStatus;
  severity: 'info' | 'warn' | 'success' | 'danger' | 'contrast';
  candidates: ApplicationListItem[];
}
