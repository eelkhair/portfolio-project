import { ResumeData } from '../types/resume-data.type';

export const MOCK_RESUME_DATA: ResumeData = {
  fullName: 'Alex Johnson',
  email: 'alex.johnson@email.com',
  phone: '+1 (555) 123-4567',
  linkedin: 'linkedin.com/in/alexjohnson',
  portfolio: 'alexjohnson.dev',
  experience:
    '5 years of full-stack development experience. Previously at Stripe (Senior Engineer) and Google (Software Engineer). Led migration of monolithic payment service to microservices architecture, reducing latency by 40%. Built real-time analytics dashboard serving 10K+ daily users.',
  skills: ['TypeScript', 'Python', 'React', 'Node.js', 'PostgreSQL', 'AWS', 'Docker', 'Kubernetes'],
};
