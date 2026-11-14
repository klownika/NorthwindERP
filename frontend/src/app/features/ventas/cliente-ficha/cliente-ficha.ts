import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { SalesApi } from '../../../core/api/sales.api';
import { Customer, CustomerOrderHistoryLine } from '../../../core/models/api.models';
import { fmtDate, int, money } from '../../../core/util/format';
import { toUiError } from '../../../core/util/problem';
import { PageService } from '../../../layout/page.service';
import { Loader } from '../../../shared/data/loader';
import { IconComponent } from '../../../shared/ui/icon';
import { LoadStateComponent } from '../../../shared/ui/load-state';
import { Pager, PagerComponent } from '../../../shared/ui/pager';

/** Sub-subinterfaz: ficha del cliente, PUT de dirección e historial (get_customer_order_history). */
@Component({
  selector: 'app-cliente-ficha',
  imports: [FormsModule, RouterLink, IconComponent, LoadStateComponent, PagerComponent],
  templateUrl: './cliente-ficha.html',
  styleUrl: './cliente-ficha.scss',
})
export class ClienteFichaComponent {
  private sales = inject(SalesApi);
  private page = inject(PageService);
  readonly id = Number(inject(ActivatedRoute).snapshot.paramMap.get('id'));

  readonly load = new Loader();
  readonly customer = signal<Customer | null>(null);
  readonly history = signal<CustomerOrderHistoryLine[]>([]);
  readonly f = { address: signal(''), city: signal(''), postalCode: signal(''), country: signal('') };
  readonly msg = signal<{ ok: boolean; title: string; text: string } | null>(null);
  readonly saving = signal(false);

  readonly money = money;
  readonly int = int;
  readonly fmtDate = fmtDate;
  readonly cols = '100px 120px 150px 160px minmax(200px, 1fr) 80px 100px 120px';

  readonly stats = computed(() => {
    const h = this.history();
    return { orders: new Set(h.map((x) => x.orderId)).size, sales: h.reduce((a, x) => a + x.lineTotal, 0) };
  });
  readonly pager = new Pager(this.history, 10);

  constructor() {
    this.load.run(forkJoin({ c: this.sales.customer(this.id), h: this.sales.customerHistory(this.id) }), (r) => {
      this.setCustomer(r.c);
      this.history.set([...r.h].sort((a, b) => b.orderDate.localeCompare(a.orderDate) || b.orderId - a.orderId));
    });
  }

  cityLine(c: Customer): string { return [c.postalCode, c.city, c.country].filter((x) => !!x).join(' · '); }

  save(): void {
    const v = { address: this.f.address().trim(), city: this.f.city().trim(), postalCode: this.f.postalCode().trim(), country: this.f.country().trim() };
    if (!v.address && !v.city && !v.postalCode && !v.country) {
      this.msg.set({ ok: false, title: '400 Bad Request', text: 'Send at least one address field to update.' });
      return;
    }
    this.saving.set(true);
    this.sales.updateCustomer(this.id, {
      address: v.address || null, city: v.city || null, postalCode: v.postalCode || null, country: v.country || null,
    }).subscribe({
      next: (c) => { this.setCustomer(c); this.msg.set({ ok: true, title: '200 OK', text: 'Dirección actualizada.' }); this.saving.set(false); },
      error: (e: unknown) => {
        const ui = toUiError(e);
        this.msg.set({ ok: false, title: ui.title, text: ui.messages.join(' ') });
        this.saving.set(false);
      },
    });
  }

  private setCustomer(c: Customer): void {
    this.customer.set(c);
    this.page.setDetail(c.customerName);
    this.f.address.set(c.address ?? ''); this.f.city.set(c.city ?? '');
    this.f.postalCode.set(c.postalCode ?? ''); this.f.country.set(c.country ?? '');
  }
}
