import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { InventoryApi } from '../../../core/api/inventory.api';
import { SalesApi } from '../../../core/api/sales.api';
import { StaffApi } from '../../../core/api/staff.api';
import { AuthService } from '../../../core/auth/auth.service';
import { Category, CreateOrderRequest, Customer, Product, Shipper, StockItem, Warehouse } from '../../../core/models/api.models';
import { int, money } from '../../../core/util/format';
import { UiError, toUiError } from '../../../core/util/problem';
import { LowStockService } from '../../../layout/low-stock.service';
import { Loader } from '../../../shared/data/loader';
import { ComboOption, ComboboxComponent } from '../../../shared/ui/combobox';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { QtyStepperComponent } from '../../../shared/ui/qty-stepper';
import { ProductPickerComponent, Selection } from './product-picker';

/** Una línea por producto: elegir de nuevo el mismo producto cambia su cantidad, no duplica la fila. */
interface Line { productId: number; quantity: number; }

/** Nuevo pedido → POST /api/orders/with-customer (sp_register_order: todo o nada). */
@Component({
  selector: 'app-nuevo-pedido',
  imports: [FormsModule, ComboboxComponent, IconComponent, LoadStateComponent, ProductPickerComponent, QtyStepperComponent],
  templateUrl: './nuevo-pedido.html',
  styleUrl: './nuevo-pedido.scss',
})
export class NuevoPedidoComponent {
  private sales = inject(SalesApi);
  private inventory = inject(InventoryApi);
  private staff = inject(StaffApi);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private lowStock = inject(LowStockService);
  readonly auth = inject(AuthService);

  readonly load = new Loader();
  readonly customers = signal<Customer[]>([]);
  readonly products = signal<Product[]>([]);
  readonly categories = signal<Category[]>([]);
  readonly warehouses = signal<Warehouse[]>([]);
  readonly shippers = signal<Shipper[]>([]);
  readonly stock = signal<StockItem[]>([]);

  readonly mode = signal<'existing' | 'new'>('existing');
  readonly customerId = signal<number>(0);
  readonly nc = { customerName: signal(''), contactName: signal(''), address: signal(''), city: signal(''), postalCode: signal(''), country: signal('') };
  readonly warehouseId = signal<number>(1);
  readonly shipperId = signal<number>(0);
  readonly lines = signal<Line[]>([]);
  readonly pickerOpen = signal(false);

  readonly busy = signal(false);
  readonly error = signal<UiError | null>(null);
  readonly money = money;

  readonly customerOpts = computed<ComboOption[]>(() => [...this.customers()]
    .sort((a, b) => a.customerName.localeCompare(b.customerName))
    .map((c) => ({ value: c.customerId, label: c.customerName, hint: c.country })));
  readonly whOpts = computed<ComboOption[]>(() => this.warehouses().map((w) => ({ value: w.warehouseId, label: w.warehouseName })));
  readonly shipperOpts = computed<ComboOption[]>(() => this.shippers().map((s) => ({ value: s.shipperId, label: s.shipperName })));
  readonly view = computed(() => {
    const w = this.warehouseId();
    const whName = this.warehouses().find((x) => x.warehouseId === w)?.warehouseName ?? '';
    return this.lines().map((l) => {
      const p = this.products().find((x) => x.productId === l.productId);
      const avail = this.stock().find((s) => s.warehouseId === w && s.productId === l.productId)?.quantity ?? 0;
      return {
        ...l, name: p?.productName ?? `Producto #${l.productId}`, max: Math.min(10000, avail),
        price: p ? money(p.price) : '—', total: p ? p.price * (l.quantity || 0) : 0,
        avail: p ? `${int(avail)} u. en ${whName}` : '—', short: !!p && l.quantity > avail,
      };
    });
  });
  readonly selection = computed<Selection>(() => Object.fromEntries(this.lines().map((l) => [l.productId, l.quantity])));
  readonly total = computed(() => this.view().reduce((a, l) => a + l.total, 0));

  readonly steps = [{ n: 1, label: 'Cliente' }, { n: 2, label: 'Almacén y envío' }, { n: 3, label: 'Productos' }];
  readonly step = signal(1);
  readonly summary = computed(() => ({
    customer: this.mode() === 'new'
      ? this.nc.customerName().trim() || '—'
      : this.customers().find((c) => c.customerId === this.customerId())?.customerName ?? '—',
    warehouse: this.warehouses().find((w) => w.warehouseId === this.warehouseId())?.warehouseName ?? '—',
    shipper: this.shippers().find((s) => s.shipperId === this.shipperId())?.shipperName ?? 'Sin asignar',
    products: this.lines().length,
  }));

  constructor() {
    this.load.run(
      forkJoin({
        c: this.sales.customers(), p: this.inventory.products(), w: this.inventory.warehouses(),
        s: this.staff.shippers(), st: this.inventory.stock(), cat: this.inventory.categories(),
      }),
      (r) => {
        this.customers.set(r.c); this.products.set(r.p); this.warehouses.set(r.w); this.shippers.set(r.s); this.stock.set(r.st);
        this.categories.set(r.cat);
        if (r.w.length) this.warehouseId.set(r.w[0].warehouseId);
        // Viene desde la fila de un cliente: queda preseleccionado.
        const preset = Number(this.route.snapshot.queryParamMap.get('customerId'));
        if (r.c.some((c) => c.customerId === preset)) this.customerId.set(preset);
      },
    );
  }

  removeLine(productId: number): void { this.lines.update((ls) => ls.filter((l) => l.productId !== productId)); }
  setQty(productId: number, quantity: number): void {
    this.lines.update((ls) => ls.map((l) => (l.productId === productId ? { ...l, quantity } : l)));
  }

  /** Aplica lo elegido en el catálogo: actualiza las filas existentes, agrega las nuevas y quita las que quedaron en 0. */
  applySelection(sel: Selection): void {
    const kept = this.lines().filter((l) => sel[l.productId]).map((l) => ({ ...l, quantity: sel[l.productId] }));
    const known = new Set(kept.map((l) => l.productId));
    const name = (id: number) => this.products().find((p) => p.productId === id)?.productName ?? '';
    const added = Object.keys(sel).map(Number).filter((id) => !known.has(id))
      .sort((a, b) => name(a).localeCompare(name(b)))
      .map((productId) => ({ productId, quantity: sel[productId] }));
    this.lines.set([...kept, ...added]);
    this.pickerOpen.set(false);
  }

  reset(): void {
    this.step.set(1); this.mode.set('existing'); this.customerId.set(0); this.shipperId.set(0); this.error.set(null);
    Object.values(this.nc).forEach((s) => s.set(''));
    this.lines.set([]);
  }

  private errorsFor(step: number): string[] {
    const errs: string[] = [];
    if (step === 1) {
      if (this.mode() === 'existing' && !this.customerId()) errs.push('Selecciona un cliente.');
      if (this.mode() === 'new' && !this.nc.customerName().trim()) errs.push('The CustomerName field is required.');
    }
    if (step === 2 && !this.warehouseId()) errs.push('Selecciona un almacén.');
    if (step === 3) {
      if (!this.lines().length) errs.push('Agrega al menos un producto.');
      if (this.lines().some((l) => !Number.isInteger(l.quantity) || l.quantity < 1 || l.quantity > 10000)) errs.push('The field Quantity must be between 1 and 10000.');
    }
    return errs;
  }

  /** Avanza solo si los pasos anteriores están completos; si no, lleva al primero que falta. */
  goTo(target: number): void {
    for (let s = 1; s < target; s++) {
      const errs = this.errorsFor(s);
      if (errs.length) {
        this.step.set(s);
        this.error.set({ title: 'REVISA EL FORMULARIO', messages: errs });
        return;
      }
    }
    this.error.set(null);
    this.step.set(target);
  }

  submit(): void {
    if (this.step() < this.steps.length) { this.goTo(this.step() + 1); return; }
    for (const s of this.steps) {
      const errs = this.errorsFor(s.n);
      if (errs.length) {
        this.step.set(s.n);
        this.error.set({ title: 'REVISA EL FORMULARIO', messages: errs });
        return;
      }
    }

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
