export interface UpdateCompanyDto {
  name: string;
  companyEmail: string;
  companyWebsite?: string;
  phone?: string;
  description?: string;
  about?: string;
  eeo?: string;
  founded?: string;
  size?: string;
  logo?: string;
  industryUId: string;
}
