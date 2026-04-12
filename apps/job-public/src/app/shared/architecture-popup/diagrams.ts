export interface ArchitectureEvent {
  title: string;
  description: string;
}

export const ARCHITECTURE_EVENTS: Record<string, ArchitectureEvent> = {
  'create-company': {
    title: 'Company Created',
    description: 'Company provisioned across all services with Keycloak user/group setup.',
  },
  'create-job': {
    title: 'Job Published',
    description: 'Job published and AI embedding generated for semantic matching.',
  },
  'resume-parse': {
    title: 'Resume Processed',
    description: 'Resume parsed by AI, embedded for matching, and match explanations generated.',
  },
};
