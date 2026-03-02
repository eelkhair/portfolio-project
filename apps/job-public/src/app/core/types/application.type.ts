export interface SubmitApplicationRequest {
  jobId: string;
  resumeId?: string;
  coverLetter?: string;
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
