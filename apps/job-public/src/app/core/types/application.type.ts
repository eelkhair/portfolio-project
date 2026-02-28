export interface ApplicationForm {
  fullName: string;
  email: string;
  phone: string;
  linkedin: string;
  portfolio: string;
  experience: string;
  coverLetter: string;
  skills: string[];
}

export type ApplicationStatus = 'idle' | 'submitting' | 'submitted' | 'error';
