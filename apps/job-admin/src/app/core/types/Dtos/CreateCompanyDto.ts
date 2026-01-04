export interface CreateCompanyDto {
  name: string;
  companyEmail: string;
  companyWebsite?: string;
  industryUId: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
}
