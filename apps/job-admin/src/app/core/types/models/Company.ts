import {BaseAuditableEntity} from './BaseAuditableEntity';

export interface Company extends BaseAuditableEntity {
  name: string;
  about?: string;
  eeo?: string;

}
