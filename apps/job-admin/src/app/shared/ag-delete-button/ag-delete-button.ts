import {Component} from '@angular/core';
import {ICellRendererAngularComp} from 'ag-grid-angular';
import {ICellRendererParams} from 'ag-grid-community';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-ag-delete-button',
  imports: [Button],
  template: `<div class="delete-btn-wrapper"><p-button icon="pi pi-trash" [rounded]="true" [text]="true" severity="danger" (onClick)="params.click()" /></div>`,
  styles: [`:host { display: flex; align-items: center; justify-content: center; height: 100%; padding: 0 8px; } .delete-btn-wrapper { display: flex; align-items: center; justify-content: center; }`],
})
export class AgDeleteButton implements ICellRendererAngularComp {
  params!: ICellRendererParams & { click: () => void };

  agInit(params: ICellRendererParams & { click: () => void }): void {
    this.params = params;
  }

  refresh(): boolean {
    return true;
  }
}
