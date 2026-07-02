import { Component, forwardRef, signal, ChangeDetectionStrategy } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR, FormsModule } from '@angular/forms';
import { InputText } from 'primeng/inputtext';
import { InputGroup } from 'primeng/inputgroup';
import { InputGroupAddon } from 'primeng/inputgroupaddon';
import { TranslocoPipe } from '@jsverse/transloco';

@Component({
  selector: 'app-phone-input',
  standalone: true,
  imports: [FormsModule, InputText, InputGroup, InputGroupAddon, TranslocoPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => PhoneInputComponent),
      multi: true
    }
  ],
  template: `
    <p-inputgroup>
      <p-inputgroup-addon>+998</p-inputgroup-addon>
      <input pInputText
             [ngModel]="displayValue()"
             (ngModelChange)="onInput($event)"
             (blur)="onTouched()"
             placeholder="88 123 45 67"
             inputmode="tel"
             autocomplete="tel"
             class="w-full" />
    </p-inputgroup>
    @if (touched() && invalid()) {
      <small class="phone-error">{{ 'auth.phoneLength' | transloco }}</small>
    }
  `,
  styles: [`
    :host { display: block; }
    .phone-error {
      color: #ef4444;
      font-size: 12px;
      margin-top: 4px;
      display: block;
    }
  `]
})
export class PhoneInputComponent implements ControlValueAccessor {
  displayValue = signal('');
  touched = signal(false);
  invalid = signal(false);

  private onChange: (value: string | null) => void = () => {};
  onTouched: () => void = () => {};

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
    const origTouched = this.onTouched;
    this.onTouched = () => {
      this.touched.set(true);
      fn();
      origTouched();
    };
  }

  onInput(value: string): void {
    // Barcha raqamlarni olamiz; to'liq raqam yopishtirilsa (998... yoki 12 xonali)
    // yetakchi 998 ni tashlab, oxirgi 9 xonani saqlaymiz.
    let digits = value.replace(/\D/g, '');
    if (digits.length > 9 && digits.startsWith('998')) digits = digits.slice(3);
    digits = digits.slice(-9);
    this.displayValue.set(digits);
    this.invalid.set(digits.length > 0 && digits.length !== 9);

    if (digits.length === 0) {
      this.onChange(null);
    } else {
      this.onChange('+998' + digits);
    }
  }
}
