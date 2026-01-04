import {BaseAuditableEntity} from './BaseAuditableEntity';

export interface Company extends BaseAuditableEntity {
  name?: string;
  description?: string | undefined;
  website?: string | undefined;
  email?: string;
  phone?: string | undefined;
  about?: string | undefined;
  eeo?: string | undefined;
  founded?: Date | undefined;
  size?: string | undefined;
  logo?: string | undefined;
  status?: string | undefined;
  isActive?: boolean;


}
