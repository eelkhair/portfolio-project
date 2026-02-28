export interface Company {
  id: string;
  name: string;
  description: string | null;
  website: string | null;
  about: string | null;
  founded: string | null;
  size: string | null;
  logo: string | null;
  industryName: string;
  jobCount: number;
}
