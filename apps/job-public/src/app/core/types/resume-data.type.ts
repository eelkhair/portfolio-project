export interface ResumeData {
  fullName: string;
  email: string;
  phone: string;
  linkedin: string;
  portfolio: string;
  experience: string;
  skills: string[];
}

export type ParseStatus = 'idle' | 'uploading' | 'parsing' | 'parsed' | 'error';
