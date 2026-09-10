import { ChangeDetectionStrategy, Component, forwardRef, signal } from '@angular/core';
import { FormsModule, NG_VALUE_ACCESSOR, type ControlValueAccessor } from '@angular/forms';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { InputText } from 'primeng/inputtext';
import { TranslocoPipe } from '@jsverse/transloco';

/** `+998` bilan telefon maydoni (eski `phone-input`). Qiymat E.164: `+998901234567`. */
@Component({
  selector: 'app-phone-input',
  imports: [FormsModule, InputText, InputGroup, InputGroupAddon, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true,
    },
  ],
  template: `
    <p-inputgroup>
      <p-inputgroup-addon>{{ prefix }}</p-inputgroup-addon>
      <input
        pInputText
        [ngModel]="displayValue()"
        (ngModelChange)="onInput($event)"
        (blur)="onTouched()"
        placeholder="88 123 45 67"
        inputmode="tel"
        autocomplete="tel"
        class="w-full"
      />
    </p-inputgroup>
    @if (touched() && invalid()) {
      <small class="phone-error">{{ 'auth.phoneLength' | transloco }}</small>
    }
  `,
  styles: `
    :host {
      display: block;
    }
    .phone-error {
      color: var(--color-berry-500);
      font-size: 12px;
      margin-top: 4px;
      display: block;
    }
  `,
})
export class PhoneInputComponent implements ControlValueAccessor {
  /** Mamlakat kodi — tarjima qilinmaydi. */
  readonly prefix = '+998';

  readonly displayValue = signal('');
  readonly touched = signal(false);
  readonly invalid = signal(false);

  private onChange: (value: string | null) => void = () => undefined;
  onTouched: () => void = () => undefined;

  writeValue(value: string | null): void {
    if (value) {
      const stripped = value.replace(/^\+?998/, '');
      this.displayValue.set(stripped);
      this.invalid.set(stripped.length > 0 && stripped.length !== 9);
    } else {
      this.displayValue.set('');
      this.invalid.set(false);
    }
  }

  registerOnChange(fn: (value: string | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = () => {
      this.touched.set(true);
      fn();
    };
  }

  onInput(value: string): void {
    // To'liq raqam yopishtirilsa (998... yoki 12 xonali) yetakchi 998 tashlanadi.
    let digits = value.replace(/\D/g, '');
    if (digits.length > 9 && digits.startsWith('998')) digits = digits.slice(3);
    digits = digits.slice(-9);
    this.displayValue.set(digits);
    this.invalid.set(digits.length > 0 && digits.length !== 9);
    this.onChange(digits.length === 0 ? null : `+998${digits}`);
  }
}
