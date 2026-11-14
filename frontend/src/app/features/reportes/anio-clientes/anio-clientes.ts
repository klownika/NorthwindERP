import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ReportsApi } from '../../../core/api/reports.api';
import { CustomerYearSales } from '../../../core/models/api.models';
import { money } from '../../../core/util/format';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Ventas por cliente y año (requisito: ventas con totales por año). */
@Component({
  selector: 'app-anio-clientes',
  imports: [FormsModule, LoadStateComponent, PagerComponent],
  template: `
    <div class="stack">
      <div class="fgroup" style="max-width: 200px">
        <label for="ryc" class="lbl">Año</label>
        <select id="ryc" class="field" [ngModel]="year()" (ngModelChange)="year.set(+$event)">
          @for (y of years(); track y) { <option [ngValue]="y">{{ y }}</option> }
        </select>
      </div>
      <app-load-state [loading]="load.loading()" [error]="load.error()" />
      <app-pager [pager]="pager" />
      <div class="table-box">
        <div style="min-width: 820px">
          <div class="thr" [style.grid-template-columns]="cols"><div>Cliente</div><div>Año</div><div class="r">Pedidos</div><div class="r">Ventas</div><div class="r">Promedio por línea</div></div>
          @for (r of pager.rows(); track r.customerId) {
            <div class="tr" [style.grid-template-columns]="cols">
              <div class="sb">{{ r.customerName }}</div><div>{{ r.salesYear }}</div><div class="r">{{ r.totalOrders }}</div>
              <div class="r b">{{ money(r.totalSales) }}</div><div class="r">{{ money(r.avgLineTotal) }}</div>
            </div>
          } @empty { @if (!load.loading()) { <div class="empty">Sin ventas en este año.</div> } }
        </div>
      </div>
    </div>
  `,
})
export class AnioClientesComponent {
  private api = inject(ReportsApi);
  readonly load = new Loader();
  readonly all = signal<CustomerYearSales[]>([]);
  readonly year = signal<number>(0);
  readonly money = money;
  readonly cols = 'minmax(240px, 1fr) 90px 100px 140px 170px';

  readonly years = computed(() => [...new Set(this.all().map((r) => r.salesYear))].sort());
  readonly rows = computed(() => this.all().filter((r) => r.salesYear === this.year()).sort((a, b) => b.totalSales - a.totalSales));
  readonly pager = new Pager(this.rows, 10);

  constructor() {
    // Sin ?year= la API devuelve todos los años; el selector filtra en el cliente.
    this.load.run(this.api.salesByCustomerYear(), (r) => {
      this.all.set(r);
      const ys = [...new Set(r.map((x) => x.salesYear))].sort();
      this.year.set(ys[ys.length - 1] ?? 0);
    });
  }
}
