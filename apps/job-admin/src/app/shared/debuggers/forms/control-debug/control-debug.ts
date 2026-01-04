import {
  Component,
  input,
  signal,
  effect,
  OnInit, WritableSignal, InputSignal
} from '@angular/core';


type DebugModel = {
  key: string;
  ready: WritableSignal<boolean>;
  show: WritableSignal<boolean>;
  parentReady: InputSignal<boolean | undefined>;
  parentShow: InputSignal<boolean | undefined>;
}

@Component({
  selector: 'app-control-debug',
  standalone: true,
  templateUrl: './control-debug.html',
})

export class ControlDebugComponent  implements OnInit {
  states = input.required<any>();
  groupName = input<string>();

  parentDirtyShow = input<boolean>();
  parentDirtyReady = input<boolean>();
  parentTouchedShow = input<boolean>();
  parentTouchedReady = input<boolean>();
  parentValueShow = input<boolean>();
  parentValueReady = input<boolean>();
  parentErrorShow = input<boolean>();
  parentErrorReady = input<boolean>();

  objectKeys = Object.keys ;

  readonly dirty = this.createDebugModel(
    'form-debug-show-dirty-',
    this.parentDirtyShow,
    this.parentDirtyReady
  );

  readonly touched = this.createDebugModel(
    'form-debug-show-touched-',
    this.parentTouchedShow,
    this.parentTouchedReady
  );

  readonly value = this.createDebugModel(
    'form-debug-show-values-',
    this.parentValueShow,
    this.parentValueReady
  );

  readonly error = this.createDebugModel(
    'form-debug-show-errors-',
    this.parentErrorShow,
    this.parentErrorReady
  );

  protected debugModels = [
    { key: 'dirty', label: 'Dirty', model: this.dirty,},
    { key: 'touched', label: 'Touched', model: this.touched },
    { key: 'value', label: 'Value', model: this.value },
    { key: 'error', label: 'Error', model: this.error }
  ];

  constructor() {
    // Reactive handling of parent-to-child propagation
    this.debugModels.forEach(({model}) => {
      effect(() => this.syncWithParent(model));
    });
  }

  ngOnInit() {
    // Initialize signals models
    this.initialize(this.dirty, true);
    this.initialize(this.touched, true);
    this.initialize(this.value, true);
    this.initialize(this.error, true);

  }

  /**
   * Determines if the given value is a "leaf" node in a form structure,
   * i.e., an object containing 'errors', 'dirty', or 'touched' properties.
   * @param value - The value to check.
   * @returns {boolean} True if the value is a leaf node, false otherwise.
   */
  protected isLeaf(value: any): boolean {
    return (
      value &&
      typeof value === 'object' &&
      ('errors' in value || 'dirty' in value || 'touched' in value)
    );
  }

  /**
   * Formats a validation error key and value into a user-friendly message.
   * @param key - The error key (e.g., 'required', 'email').
   * @param value - The error value/details.
   * @returns {string} A formatted error message.
   */
  protected formatError(key: string, value: any): string {
    const descriptions: Record<string, string> = {
      required: 'This field is required.',
      email: 'Must be a valid email.',
      minlength: `Too short.`,
      maxlength: `Too long.`,
      valuesNotEqual: 'Passwords must match.',
      matchValues: `Passwords must match.`,
    };
    return descriptions[key] ?? `${key}: ${JSON.stringify(value)}`;
  }

  /**
   * Initializes the 'show' signal in the given DebugModel based on a value from localStorage or its parent signal.
   * If a value is found in localStorage, sets 'show' to true if the value is '1', otherwise false.
   * If not found, falls back to the parent signal's value, or to the provided initial value if the parent signal is undefined.
   *
   * @param model - The DebugModel instance to initialize.
   * @param initialValue - The fallback value to use if neither localStorage nor the parent signal provides a value (default: false).
   */
  private initialize(model: DebugModel, initialValue: boolean = false): void {
    const stored = this.getStorageItem(model.key);
    model.show.set(
      stored != null
        ? stored === '1'
        : model.parentShow?.() ?? initialValue,
    );
  }

  /**
   * Updates the child signal based on the parent signal if the parent is ready,
   * and persists the value to localStorage.
   * @param model - The DebugModel instance containing signals and key.
   */
  private syncWithParent(model: DebugModel) {
    if (model.parentReady?.() !== undefined && model.parentReady?.()!) {
      model.show.set(model.parentShow?.() ?? false);
      this.setStorageItem(model.key, model.parentShow?.() ? '1' : '0');
    }
  }

  /**
   * Builds a storage key by combining the base key with the current group name.
   * If groupName is not set, 'root' is used as the default.
   * @param base - The base key to use.
   * @returns {string} The combined storage key.
   */
  private getStorageKey(base: string): string {
    return base + (this.groupName?.() ?? 'root');
  }

  /**
   * Gets an item from localStorage by key.
   * @param key - the base key to look up
   * @returns {string|null} The stored value or null if not found
   */
  private getStorageItem(key: string): string|null {
    return localStorage.getItem(this.getStorageKey(key));
  }

  /**
   * Sets an item in localStorage using the combined storage key.
   * @param key - The base key to use.
   * @param value - The value to store.
   */
  private setStorageItem(key: string, value: string): void {
    localStorage.setItem(this.getStorageKey(key), value);
  }

  /**
   * Creates a DebugModel instance for a specific form aspect (e.g., dirty, touched, error).
   *
   * @param key - The unique key used for localStorage and identification.
   * @param parentShow - The input signal representing the parent "show" state.
   * @param parentReady - The input signal representing the parent "ready" state.
   * @returns {DebugModel} A new DebugModel instance with initialized signals.
   */
  private createDebugModel(
    key: string,
    parentShow: InputSignal<boolean | undefined>,
    parentReady: InputSignal<boolean | undefined>
  ): DebugModel {
    return {
      key,
      ready: signal(false),
      show: signal(false),
      parentReady,
      parentShow,
    };
  }
}
