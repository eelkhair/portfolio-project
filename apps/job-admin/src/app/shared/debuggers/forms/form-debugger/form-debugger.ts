import {Component, DestroyRef, inject, input, OnInit, signal} from '@angular/core';
import {UpperCasePipe} from "@angular/common";
import {AbstractControl, FormArray, FormControl, FormGroup} from '@angular/forms';
import {ControlDebugComponent} from '../control-debug/control-debug';
import {Button} from 'primeng/button';
import {Divider} from 'primeng/divider';


@Component({
  selector: 'app-form-debugger',
  imports: [
    UpperCasePipe,
    ControlDebugComponent,
    Button,
    Divider
  ],
  templateUrl: './form-debugger.html',
  styleUrl: './form-debugger.css'
})
export class FormDebugger implements OnInit {
  private destroyRef = inject(DestroyRef);
  form = input.required<FormGroup>({})
  title = input<string>();
  controlStates = signal<any>(null);
  protected fullMode = signal<boolean>(false)

  ngOnInit() {
    const subscription = this.form().valueChanges.subscribe(() => {
      if (this.fullMode()) {
        this.controlStates.set(this.getControlStates(this.form()));
      }
    })
    this.destroyRef.onDestroy(() => subscription.unsubscribe());
  }

  toggleFullMode() {
    this.fullMode.set(!this.fullMode())
    this.controlStates.set(this.getControlStates(this.form()));
  }

  getControlStates(control: AbstractControl): any {
    // Helper to extract state if relevant
    const extractState = (ctrl: AbstractControl) =>
      (ctrl.errors || ctrl.dirty || ctrl.touched || ctrl.value)?
        {errors: ctrl.errors ?? null, dirty: ctrl.dirty, touched: ctrl.touched, value:ctrl.value, pending: ctrl.pending, disabled:ctrl.disabled}
        : null;

    if (control instanceof FormGroup) {
      const result: any = {};
      for (const [key, child] of Object.entries(control.controls)) {
        const state = this.getControlStates(child);
        if (state !== null) result[key] = state;
      }
      const groupState = extractState(control);
      if (groupState) result['$group'] = groupState;
      return Object.keys(result).length ? result : null;
    }

    if (control instanceof FormArray) {
      const states = control.controls
        .map(child => this.getControlStates(child))
        .filter(Boolean);
      return states.length ? states : null;
    }

    if (control instanceof FormControl) {
      return extractState(control);
    }

    return null;
  }
}
