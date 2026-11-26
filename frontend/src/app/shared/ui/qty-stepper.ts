import { Component, input, output } from '@angular/core';

/** Control de cantidad estilo tienda: − número +. Uso: <app-qty-stepper [value]="n" [max]="stock" label="Chai" (valueChange)="set($event)" /> */
@Component({
  selector: 'app-qty-stepper',
  template: `
    <div class="qty" role="group" [attr.aria-label]="'Cantidad de ' + label()">
      <button type="button" class="step" [disabled]="disabled() || value() <= min()" [attr.aria-label]="'Quitar una unidad de ' + label()"
              (click)="valueChange.emit(value() - 1)">−</button>
      <output class="n" aria-live="polite">{{ value() }}</output>
      <button type="button" class="step" [disabled]="disabled() || value() >= max()" [attr.aria-label]="'Agregar una unidad de ' + label()"
              [title]="value() >= max() ? 'No hay más stock disponible' : ''" (click)="valueChange.emit(value() + 1)">+</button>
    </div>
  `,
  styles: `
    :host { display: inline-flex; }
    .qty { display: inline-flex; align-items: center; gap: 4px; height: 44px; padding: 3px; border: 1px solid #e9c8ae; border-radius: 999px; background: #fff; }
    .step {
      width: 36px; height: 36px; display: inline-flex; align-items: center; justify-content: center; padding: 0; cursor: pointer;
      border: 1px solid #f2c4a0; border-radius: 999px; background: linear-gradient(180deg, #fff, #fff0e2);
      color: var(--or-800); font-size: 20px; font-weight: 800; line-height: 1;
    }
    .step:hover:not(:disabled) { border-color: var(--or-400); }
    .step:disabled { opacity: .4; cursor: not-allowed; }
    .n { min-width: 36px; text-align: center; font-family: var(--font-display); font-size: 16px; font-weight: 800; color: var(--ink); }
  `,
})
export class QtyStepperComponent {
  readonly value = input.required<number>();
  readonly min = input(0);
  readonly max = input(10000);
  readonly label = input('');
  readonly disabled = input(false);
  readonly valueChange = output<number>();
}
