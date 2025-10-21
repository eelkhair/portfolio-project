import {Component, inject, OnInit, signal} from '@angular/core';
import {DynamicDialogConfig, DynamicDialogRef} from 'primeng/dynamicdialog';
import {Checkbox} from 'primeng/checkbox';
import {FormsModule} from '@angular/forms';
import {Button} from 'primeng/button';

@Component({
  selector: 'app-job-publish-confirm',
  imports: [
    Checkbox,
    FormsModule,
    Button
  ],
  templateUrl: './job-publish-confirm.html',
  styleUrl: './job-publish-confirm.css'
})
export class JobPublishConfirm implements OnInit {
  confirmRef = inject(DynamicDialogRef);
  config = inject(DynamicDialogConfig);
  deleteDraft = signal(false);
  isDraft =signal(false)

  ngOnInit(): void {
    if(this.config.data.draftId){
      this.deleteDraft.set(true);
      this.isDraft.set(true);
    }
  }
  submit() {
    this.confirmRef.close(this.deleteDraft());
  }

  cancel() {
    this.confirmRef.close(undefined);
  }
}
