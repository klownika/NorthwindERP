import { Component, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { SalesApi } from '../../../core/api/sales.api';
import { StaffApi } from '../../../core/api/staff.api';
import { OrderSummary, Shipper } from '../../../core/models/api.models';
import { Loader } from '../../../shared/data/loader';
import { GlossyIconComponent } from '../../../shared/ui/glossy-icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';

/** Transportistas y cuántos pedidos llevan. */
@Component({
  selector: 'app-transportistas',
  imports: [GlossyIconComponent, LoadStateComponent],
  template: `
    <app-load-state [loading]="load.loading()" [error]="load.error()" />
    <div class="grid-auto" style="--min: 260px">
      @for (s of cards(); track s.shipperId) {
        <article class="card sh">
          <span class="ico"><app-gicon name="truck" [size]="48" /></span>
          <h2>{{ s.shipperName }}</h2>
          <span class="txt">Teléfono: {{ s.phone ?? '—' }}</span>
          <span class="txt">Pedidos transportados: <strong>{{ s.orders }}</strong></span>
        </article>
      }
    </div>
  `,
  styles: `
    .sh { padding: 20px; display: flex; flex-direction: column; gap: 10px; }
    .ico { display: inline-flex; align-self: flex-start; }
    h2 { margin: 0; font-size: 17px; font-weight: 800; }
    .txt { font-size: 14px; color: var(--ink-2); }
  `,
})
export class TransportistasComponent {
  private staff = inject(StaffApi);
  private sales = inject(SalesApi);
  readonly load = new Loader();
  readonly shippers = signal<Shipper[]>([]);
  readonly orders = signal<OrderSummary[]>([]);

  readonly cards = computed(() =>
    this.shippers().map((s) => ({ ...s, orders: this.orders().filter((o) => o.shipperName === s.shipperName).length })),
  );

  constructor() {
    this.load.run(forkJoin({ s: this.staff.shippers(), o: this.sales.orders() }), (r) => {
      this.shippers.set(r.s);
      this.orders.set(r.o);
    });
  }
}
