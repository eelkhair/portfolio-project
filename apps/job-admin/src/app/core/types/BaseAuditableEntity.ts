import {BaseEntity} from './BaseEntity';

export interface BaseAuditableEntity extends BaseEntity {
  createdAt: string;
  updatedAt: string;
}
