import { Component, ElementRef, computed, effect, input, output, signal, untracked, viewChild } from '@angular/core';
import { Category, Product, StockItem } from '../../../core/models/api.models';
import { int, money } from '../../../core/util/format';
import { CatalogBrowserComponent, CatalogItem } from '../../../shared/catalog/catalog-browser';
import { IconComponent } from '../../../shared/ui/icon';
import { QtyStepperComponent } from '../../../shared/ui/qty-stepper';

/** productId -> cantidad elegida. */
export type Selection = Record<number, number>;

/**
 * Ventana modal con el catálogo (la misma vista de Inventario > Catálogo) para elegir productos y cantidades.
 * Muestra el stock del almacén del pedido. "Guardar selección" aplica; Cancelar, la X o Esc descartan.
 */
@Component({
  selector: 'app-product-picker',
  imports: [CatalogBrowserComponent, IconComponent, QtyStepperComponent],
  template: `
    <dialog #dlg aria-labelledby="pp-title" (cancel)="$event.preventDefault(); cancelled.emit()">
      @if (open()) {
        <header class="head">
          <div>
            <h2 id="pp-title">Catálogo de productos</h2>
            <span class="muted">Stock del almacén <strong>{{ warehouseName() }}</strong></span>
          </div>
          <button type="button" class="iconbtn" aria-label="Cerrar sin guardar" title="Cerrar" (click)="cancelled.emit()"><app-icon name="close" /></button>
        </header>

        <div class="body">
          <app-catalog-browser [products]="products()" [categories]="categories()" [stock]="stock()" [warehouseId]="warehouseId()"
                               [blockEmpty]="true" [(query)]="query">
            <ng-template #cardFooter let-p>
              @if (qtyOf(p.productId); as n) {
                <app-qty-stepper [value]="n" [max]="p.qty" [label]="p.productName" (valueChange)="setQty(p, $event)" />
              } @else {
                <button type="button" class="primary add" [disabled]="p.blocked" (click)="setQty(p, 1)"
                        [attr.aria-label]="'Agregar ' + p.productName">{{ p.blocked ? 'Sin stock' : 'Agregar' }}</button>
              }
            </ng-template>
          </app-catalog-browser>
        </div>

        <footer class="foot">
          <div class="sum" aria-live="polite">
            <span><strong>{{ totals().products }}</strong> {{ totals().products === 1 ? 'producto' : 'productos' }} · {{ int(totals().units) }} u.</span>
            <span class="total">Total <strong>{{ money(totals().amount) }}</strong></span>
          </div>
          <div class="btns">
            <button type="button" class="ghost" (click)="cancelled.emit()">Cancelar</button>
            <button type="button" class="primary" (click)="saved.emit(draft())">Guardar selección</button>
          </div>
        </footer>
      }
    </dialog>
  `,
  styles: `
    :host { display: contents; }
    dialog {
      width: min(1240px, calc(100vw - 48px)); height: min(900px, calc(100dvh - 48px)); max-width: none; max-height: none; padding: 0;
      border: 0; border-radius: 18px; color: var(--ink); background: #fff; box-shadow: 0 24px 60px -20px rgba(28, 25, 23, .55);
    }
    dialog[open] { display: flex; flex-direction: column; }
    dialog::backdrop { background: rgba(28, 25, 23, .45); }
    .head { flex: 0 0 auto; display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 14px 22px; border-bottom: 1px solid var(--line); }
    h2 { margin: 0 0 2px; font-size: 19px; font-weight: 800; color: var(--or-800); }
    .body { flex: 1 1 auto; min-height: 0; overflow-y: auto; padding: 18px 22px 24px; }
    .foot {
      flex: 0 0 auto; display: flex; flex-wrap: wrap; align-items: center; justify-content: space-between; gap: 12px 20px;
      padding: 14px 22px; border-top: 1px solid var(--line); background: #fffbf7;
    }
    .sum { display: flex; flex-wrap: wrap; align-items: baseline; gap: 6px 20px; font-size: 14px; color: var(--ink-2); }
    .total strong { font-family: var(--font-display); font-size: 20px; color: var(--or-800); }
    .btns { display: flex; gap: 8px; margin-left: auto; }
    .add { min-height: 44px; padding: 0 22px; }
    @media (max-width: 720px) {
      dialog { width: 100vw; height: 100dvh; border-radius: 0; }
      .head, .body, .foot { padding-inline: 16px; }
      .btns { width: 100%; > * { flex: 1 1 0; } }
    }
  `,
})
export class ProductPickerComponent {
  private dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');

  readonly open = input.required<boolean>();
  readonly products = input<Product[]>([]);
  readonly categories = input<Category[]>([]);
  readonly stock = input<StockItem[]>([]);
  readonly warehouseId = input(0);
  readonly warehouseName = input('');
  /** Lo ya elegido en el pedido: se precarga al abrir. */
  readonly initial = input<Selection>({});
  readonly saved = output<Selection>();
  readonly cancelled = output<void>();

  readonly draft = signal<Selection>({});
  readonly query = signal('');
  readonly money = money;
  readonly int = int;

  readonly totals = computed(() => {
    const entries = Object.entries(this.draft());
    const price = new Map(this.products().map((p) => [p.productId, p.price]));
    return {
      products: entries.length,
      units: entries.reduce((a, [, q]) => a + q, 0),
      amount: entries.reduce((a, [id, q]) => a + (price.get(+id) ?? 0) * q, 0),
    };
  });

  constructor() {
    effect(() => {
      const el = this.dlg().nativeElement;
      if (this.open() && !el.open) {
        this.draft.set({ ...untracked(this.initial) });
        el.showModal();
        document.body.style.overflow = 'hidden';
      } else if (!this.open() && el.open) {
        el.close();
        document.body.style.overflow = '';
      }
    });
  }

  qtyOf(productId: number): number { return this.draft()[productId] ?? 0; }

  /** Cantidad 0 = el producto sale de la selección. Nunca supera el stock del almacén. */
  setQty(p: CatalogItem, qty: number): void {
    const n = Math.max(0, Math.min(qty, p.qty));
    this.draft.update((d) => {
      const next = { ...d };
      if (n > 0) next[p.productId] = n; else delete next[p.productId];
      return next;
    });
  }
}
