import {Injectable} from '@angular/core';
import {EnhancementRequest} from '../../core/types/Dtos/EnhancementDto';

export interface EnhancementModel {
  title?: string,
  aboutRole?: string,
  responsibilities?: string[],
  qualifications?: string[],
}

@Injectable({providedIn: 'root'})
export class JobAIEnhancerStore{
  buildModel(field: string, value:string, model: EnhancementModel): EnhancementRequest|undefined {
    const record: Record<string, string | string[]> = Object.entries(model)
      .filter(([_, value]) => value !== undefined && value !== null && value.length > 0)
      .reduce((acc, [key, value]) => {
        acc[key] = value as string | string[];
        return acc;
      }, {} as Record<string, string | string[]>);
    if(value.length < 3) return undefined;
    return {
      context: record,
      field,
      value: value,
    };

  }
}
