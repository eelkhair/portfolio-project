import { ResumeData } from '../types/resume-data.type';

export const MOCK_RESUME_DATA: ResumeData = {
  firstName: 'Alex',
  lastName: 'Johnson',
  email: 'alex.johnson@email.com',
  phone: '+1 (555) 123-4567',
  linkedin: 'linkedin.com/in/alexjohnson',
  portfolio: 'alexjohnson.dev',
  skills: ['TypeScript', 'Python', 'React', 'Node.js', 'PostgreSQL', 'AWS', 'Docker', 'Kubernetes'],
  workHistory: [
    {
      company: 'Stripe',
      jobTitle: 'Senior Software Engineer',
      startDate: '2021-03-01',
      endDate: '',
      description: 'Led migration of monolithic payment service to microservices architecture, reducing latency by 40%. Built real-time analytics dashboard serving 10K+ daily users.',
      isCurrent: true,
    },
    {
      company: 'Google',
      jobTitle: 'Software Engineer',
      startDate: '2018-06-01',
      endDate: '2021-02-28',
      description: 'Developed and maintained core search infrastructure components. Optimized query processing pipeline resulting in 15% throughput improvement.',
      isCurrent: false,
    },
  ],
  education: [
    {
      institution: 'Stanford University',
      degree: 'Master of Science',
      fieldOfStudy: 'Computer Science',
      startDate: '2016-09-01',
      endDate: '2018-06-01',
    },
    {
      institution: 'University of California, Berkeley',
      degree: 'Bachelor of Science',
      fieldOfStudy: 'Computer Science',
      startDate: '2012-09-01',
      endDate: '2016-06-01',
    },
  ],
  certifications: [
    {
      name: 'AWS Solutions Architect - Professional',
      issuingOrganization: 'Amazon Web Services',
      issueDate: '2023-01-15',
      expirationDate: '2026-01-15',
      credentialId: 'AWS-SAP-2023-0142',
    },
    {
      name: 'Certified Kubernetes Administrator',
      issuingOrganization: 'Cloud Native Computing Foundation',
      issueDate: '2022-06-01',
      expirationDate: '2025-06-01',
      credentialId: 'CKA-2200-0098',
    },
  ],
};
