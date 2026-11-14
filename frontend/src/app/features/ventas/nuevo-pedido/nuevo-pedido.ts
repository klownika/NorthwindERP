import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { SalesApi } from '../../../core/api/sales.api';
import { StaffApi } from '../../../core/api/staff.api';
import { AuthService } from '../../../core/auth/auth.service';
import { CreateOrderRequest, Customer, Product, Shipper, StockItem, Warehouse } from '../../../core/models/api.models';
import { int, money } from '../../../core/util/format';
import { UiError, toUiError } from '../../../core/util/problem';
import { LowStockService } from '../../../layout/low-stock.service';
import { Loader } from '../../../shared/data/loader';
import { LoadStateComponent } from '../../../shared/ui/load-state';

interface Line { key: number; productId: number; quantity: number; }

/** Nuevo pedido → POST /api/orders/with-customer (sp_register_order: todo o nada). */
@Component({
  selector: 'app-nuevo-pedido',
  imports: [FormsModule, LoadStateComponent],
  templateUrl: './nuevo-pedido.html',
  styleUrl: './nuevo-pedido.scss',
})
export class NuevoPedidoComponent {
  private sales = inject(SalesApi);
  private inventory = inject(InventoryApi);
  private staff = inject(StaffApi);
  private router = inject(Router);
  private lowStock = inject(LowStockService);
  readonly auth = inject(AuthService);

  readonly load = new Loader();
  readonly customers = signal<Customer[]>([]);
  readonly products = signal<Product[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly shippers = signal<Shipper[]>([]);
  readonly stock = signal<StockItem[]>([]);

  readonly mode = signal<'existing' | 'new'>('existing');
  readonly customerId = signal<number>(0);
  readonly nc = { customerName: signal(''), contactName: signal(''), address: signal(''), city: signal(''), postalCode: signal(''), country: signal('') };
  readonly warehouseId = signal<number>(1);
  readonly shipperId = signal<number>(0);
  readonly lines = signal<Line[]>([{ key: 1, productId: 0, quantity: 1 }]);
  private nextKey = 2;

  readonly busy = signal(false);
  readonly error = signal<UiError | null>(null);
  readonly money = money;

  readonly sortedCustomers = computed(() => [...this.customers()].sort((a, b) => a.customerName.localeCompare(b.customerName)));
  readonly sortedProducts = computed(() => [...this.products()].sort((a, b) => a.productName.localeCompare(b.productName)));
  readonly view = computed(() => {
    const w = this.warehouseId();
    const whName = this.warehouses().find((x) => x.warehouseId === w)?.warehouseName ?? '';
    return this.lines().map((l) => {
      const p = this.products().find((x) => x.productId === l.productId);
      const avail = this.stock().find((s) => s.warehouseId === w && s.productId === l.productId)?.quantity ?? 0;
      return {
        ...l, price: p ? money(p.price) : '—', total: p ? p.price * (l.quantity || 0) : 0,
        avail: p ? `${int(avail)} u. en ${whName}` : '—', short: !!p && l.quantity > avail,
      };
    });
  });
  readonly total = computed(() => this.view().reduce((a, l) => a + l.total, 0));

  constructor() {
    this.load.run(
      forkJoin({
        c: this.sales.customers(), p: this.inventory.products(), w: this.inventory.warehouses(),
        s: this.staff.shippers(), st: this.inventory.stock(),
      }),
      (r) => {
        this.customers.set(r.c); this.products.set(r.p); this.warehouses.set(r.w); this.shippers.set(r.s); this.stock.set(r.st);
        if (r.w.length) this.warehouseId.set(r.w[0].warehouseId);
      },
    );
  }

  addLine(): void { this.lines.update((ls) => [...ls, { key: this.nextKey++, productId: 0, quantity: 1 }]); }
  removeLine(key: number): void { this.lines.update((ls) => ls.filter((l) => l.key !== key)); }
  patchLine(key: number, patch: Partial<Line>): void {
    this.lines.update((ls) => ls.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  }

  reset(): void {
    this.mode.set('existing'); this.customerId.set(0); this.shipperId.set(0); this.error.set(null);
    Object.values(this.nc).forEach((s) => s.set(''));
    this.lines.set([{ key: this.nextKey++, productId: 0, quantity: 1 }]);
  }

  submit(): void {
    const errs: string[] = [];
    if (this.mode() === 'existing' && !this.customerId()) errs.push('Selecciona un cliente.');
    if (this.mode() === 'new' && !this.nc.customerName().trim()) errs.push('The CustomerName field is required.');
    if (this.lines().some((l) => !l.productId)) errs.push('Selecciona un producto en cada línea.');
    if (this.lines().some((l) => !Number.isInteger(l.quantity) || l.quantity < 1 || l.quantity > 10000)) errs.push('The field Quantity must be between 1 and 10000.');
    if (errs.length) { this.error.set({ title: 'REVISA EL FORMULARIO', messages: errs }); return; }

    const opt = (s: string) => s.trim() || null;
    const body: CreateOrderRequest = {
      customerId: this.mode() === 'existing' ? this.customerId() : null,
      newCustomer: this.mode() === 'new' ? {
        customerName: this.nc.customerName().trim(), contactName: opt(this.nc.contactName()), address: opt(this.nc.address()),
        city: opt(this.nc.city()), postalCode: opt(this.nc.postalCode()), country: opt(this.nc.country()),
      } : null,
      warehouseId: this.warehouseId(),
      shipperId: this.shipperId() || null,
      items: this.lines().map((l) => ({ productId: l.productId, quantity: l.quantity })),
    };

    this.busy.set(true);
    this.error.set(null);
    this.sales.createOrder(body).subscribe({
      next: (r) => {
        this.lowStock.refresh();
        this.router.navigate(['/ventas/pedidos', r.orderId], {
          state: { created: `Pedido registrado por ${money(r.orderTotal)}. Stock descontado del almacén y movimientos OUTBOUND en el Kardex.` },
        });
      },
      error: (e: unknown) => { this.error.set(toUiError(e)); this.busy.set(false); },
    });
  }
}
