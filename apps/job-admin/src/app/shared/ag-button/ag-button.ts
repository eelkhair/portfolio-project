import {Component} from '@angular/core';
import {ICellRendererAngularComp} from 'ag-grid-angular';
import { ICellRendererParams } from 'ag-grid-community';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-ag-button',
  imports: [
    Button
  ],
  templateUrl: './ag-button.html',
  styleUrl: './ag-button.css',
})
export class AgButton implements ICellRendererAngularComp {
    params!:ICellRendererParams& {
      click: () => void;
    };
    agInit(params: ICellRendererParams & {
      click: () => void;
    }): void {
      this.params = params;
    }
    refresh(): boolean {
      return true;
    }

}
