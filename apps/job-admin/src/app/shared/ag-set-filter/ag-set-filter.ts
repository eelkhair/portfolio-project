import {Component} from '@angular/core';
import {IFilterAngularComp} from 'ag-grid-angular';
import {IDoesFilterPassParams, IFilterParams} from 'ag-grid-community';
import {Select} from 'primeng/select';
import {FormsModule} from '@angular/forms';

export interface AgSetFilterParams {
  labelMap?: Record<string, string>;
}

@Component({
  selector: 'app-ag-set-filter',
  imports: [Select, FormsModule],
  template: `
    <div style="padding: 8px; min-width: 200px; min-height: 60px;">
      <p-select
        [options]="options"
        [(ngModel)]="selected"
        placeholder="All"
        [showClear]="true"
        (onChange)="onSelectionChange()"
        appendTo="body"
        [style]="{ width: '100%' }"
      />
    </div>
  `,
})
export class AgSetFilter implements IFilterAngularComp {
  params!: IFilterParams & AgSetFilterParams;
  options: { label: string; value: string }[] = [];
  selected: string | null = null;
  private labelMap: Record<string, string> = {};

  agInit(params: IFilterParams & AgSetFilterParams): void {
    this.params = params;
    this.labelMap = params.labelMap ?? {};
    this.buildOptions();
  }

  isFilterActive(): boolean {
    return this.selected != null;
  }

  doesFilterPass(params: IDoesFilterPassParams): boolean {
    const value = this.params.getValue(params.node);
    return value === this.selected;
  }

  getModel(): string | null {
    return this.selected;
  }

  setModel(model: string | null): void {
    this.selected = model;
  }

  onSelectionChange(): void {
    this.params.filterChangedCallback();
  }

  onNewRowsLoaded(): void {
    this.buildOptions();
  }

  private buildOptions(): void {
    const values = new Set<string>();
    this.params.api.forEachNode(node => {
      const val = this.params.getValue(node);
      if (val != null && val !== '') {
        values.add(String(val));
      }
    });
    this.options = Array.from(values).sort().map(v => ({
      label: this.labelMap[v] ?? v,
      value: v,
    }));
  }
}
