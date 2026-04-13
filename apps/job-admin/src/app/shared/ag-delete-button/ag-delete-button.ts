import {Component} from '@angular/core';
import {ICellRendererAngularComp} from 'ag-grid-angular';
import {ICellRendererParams} from 'ag-grid-community';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-ag-delete-button',
  imports: [Button],
  templateUrl: './ag-delete-button.html',
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
