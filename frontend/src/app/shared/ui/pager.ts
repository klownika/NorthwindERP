import { Component, Signal, WritableSignal, computed, input, linkedSignal } from '@angular/core';

/**
 * Paginación en el cliente.
 * Vuelve a la página 1 cada vez que cambia la lista de origen (por ejemplo, al filtrar).
 *   readonly pager = new Pager(this.filtered, 10);
 *   @for (row of pager.rows(); ...)
 */
export class Pager<T> {
  readonly page: WritableSignal<number>;
  readonly total = computed(() => this.source().length);
  readonly pages = computed(() => Math.max(1, Math.ceil(this.total() / this.size)));
  readonly current = computed(() => Math.min(this.page(), this.pages()));
  readonly rows = computed(() => {
    const start = (this.current() - 1) * this.size;
    return this.source().slice(start, start + this.size);
  });
  readonly label = computed(() => {
    const t = this.total();
    if (!t) return 'Sin resultados';
    const from = (this.current() - 1) * this.size + 1;
    return `Mostrando ${from}–${Math.min(t, this.current() * this.size)} de ${t}`;
  });

  constructor(private readonly source: Signal<T[]>, readonly size = 10) {
    this.page = linkedSignal({ source: this.source, computation: () => 1 });
  }

  prev(): void { if (this.current() > 1) this.page.set(this.current() - 1); }
  next(): void { if (this.current() < this.pages()) this.page.set(this.current() + 1); }
}

/** Barra "Mostrando 1–10 de 77 · ‹ Anterior 1 / 8 Siguiente ›". */
@Component({
  selector: 'app-pager',
  template: `
    <div class="pager">
      <span class="muted sb">{{ pager().label() }}</span>
      <div class="pager-ctrl">
        <button type="button" class="ghost" (click)="pager().prev()" [disabled]="pager().current() <= 1">‹ Anterior</button>
        <span class="pos">{{ pager().current() }} / {{ pager().pages() }}</span>
        <button type="button" class="primary" (click)="pager().next()" [disabled]="pager().current() >= pager().pages()">Siguiente ›</button>
      </div>
    </div>
  `,
  styles: `
    .pager { display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 10px; }
    .pager-ctrl { display: flex; align-items: center; gap: 8px; }
    .pager-ctrl .primary { min-height: 40px; }
    .pos { min-width: 70px; text-align: center; font-family: var(--font-display); font-weight: 800; font-size: 15px; color: var(--or-700); }
  `,
})
export class PagerComponent {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  readonly pager = input.required<Pager<any>>();
}
