import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';
import { UiError } from '../../core/util/problem';
import { IconComponent } from './icon';

let nextId = 0;

/** Confirmación de una acción destructiva. El foco inicia en Cancelar y Escape la cierra. */
@Component({
  selector: 'app-confirm-dialog',
  imports: [IconComponent],
  template: `
    <dialog #dlg [attr.aria-labelledby]="titleId" (cancel)="onCancel($event)" (keydown.escape)="$event.stopPropagation()">
      <span class="ico" aria-hidden="true"><app-icon name="trash" [size]="22" /></span>
      <h2 [id]="titleId">{{ title() }}</h2>
      <p>{{ message() }}</p>
      @if (error(); as e) {
        <div class="alert err" role="alert"><strong>{{ e.title }}</strong>@for (m of e.messages; track m) { <span>{{ m }}</span> }</div>
      }
      <div class="btns">
        <button type="button" class="ghost" autofocus [disabled]="busy()" (click)="cancelled.emit()">Cancelar</button>
        <button type="button" class="danger" [disabled]="busy()" (click)="confirmed.emit()">{{ busy() ? 'Eliminando…' : confirmLabel() }}</button>
      </div>
    </dialog>
  `,
  styles: `
    dialog { width: min(440px, calc(100vw - 32px)); padding: 24px; border: 0; border-radius: 18px; color: var(--ink); box-shadow: 0 24px 60px -20px rgba(28, 25, 23, .55); }
    dialog::backdrop { background: rgba(28, 25, 23, .45); }
    .ico { display: inline-flex; align-items: center; justify-content: center; width: 44px; height: 44px; border-radius: 12px; background: var(--bad-bg); color: var(--bad-ink); }
    h2 { margin: 14px 0 6px; font-size: 19px; font-weight: 800; }
    p { margin: 0 0 16px; font-size: 14px; line-height: 1.5; color: var(--ink-3); }
    .alert { margin-bottom: 16px; }
    .btns { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
  `,
})
export class ConfirmDialogComponent {
  private dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly titleId = `confirm-title-${nextId++}`;
  readonly open = input.required<boolean>();
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input('Sí, eliminar');
  readonly busy = input(false);
  readonly error = input<UiError | null>(null);
  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  constructor() {
    effect(() => {
      const el = this.dlg().nativeElement;
      if (this.open() && !el.open) el.showModal();
      else if (!this.open() && el.open) el.close();
    });
  }

  onCancel(e: Event): void {
    e.preventDefault();
    if (!this.busy()) this.cancelled.emit();
  }
}
