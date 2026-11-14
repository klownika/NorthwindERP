import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, NavigationStart, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { IconComponent } from '../../shared/ui/icon';
import { LowStockService } from '../low-stock.service';
import { NAV, NavModule, PageData } from '../nav';
import { PageService } from '../page.service';

const MINI_KEY = 'stockin.sidebar.mini';

/** Estructura fija de la app: sidebar + header + franja DORfic + contenido de cada módulo. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, IconComponent],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
})
export class ShellComponent {
  private router = inject(Router);
  readonly auth = inject(AuthService);
  readonly page = inject(PageService);
  readonly lowStock = inject(LowStockService);

  readonly nav = NAV;
  readonly mini = signal(localStorage.getItem(MINI_KEY) === '1');
  readonly open = signal<Record<string, boolean>>({ inventario: true });
  readonly search = signal('');

  /** URL actual (sin query) como signal. */
  readonly url = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      map(() => this.router.url.split('?')[0]),
      startWith(this.router.url.split('?')[0]),
    ),
    { initialValue: '/' },
  );

  /** `data` de la ruta más profunda (título, subtítulo, fuente, módulo). */
  readonly data = toSignal(
    this.router.events.pipe(
      filter((e) => e instanceof NavigationEnd),
      startWith(null),
      map(() => {
        let s = this.router.routerState.snapshot.root;
        while (s.firstChild) s = s.firstChild;
        return s.data as PageData;
      }),
    ),
    { initialValue: {} as PageData },
  );

  readonly currentModule = computed<NavModule | undefined>(() => this.nav.find((m) => m.key === this.data().module));
  readonly currentSub = computed(() => this.currentModule()?.subs.find((s) => this.url().startsWith(s.path)));
  readonly title = computed(() => this.page.titleOverride() ?? this.data().title ?? '');

  readonly me = computed(() => {
    const s = this.auth.session();
    const name = s?.username ?? '';
    return { name, role: s?.role ?? '', initials: name.slice(0, 2).toUpperCase() };
  });

  constructor() {
    this.lowStock.refresh();
    // Al salir de una pantalla se limpia el título de detalle (lo vuelve a poner la siguiente si lo necesita).
    this.router.events.pipe(filter((e) => e instanceof NavigationStart)).subscribe(() => this.page.clear());
    // Abre automáticamente el módulo de la pantalla actual.
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      const m = this.currentModule();
      if (m && !this.open()[m.key]) this.open.update((o) => ({ ...o, [m.key]: true }));
    });
  }

  isActive(path: string): boolean { return this.url().startsWith(path); }

  toggleMini(): void {
    this.mini.update((v) => !v);
    localStorage.setItem(MINI_KEY, this.mini() ? '1' : '0');
  }

  toggleModule(key: string): void {
    if (this.mini()) {
      this.mini.set(false);
      localStorage.setItem(MINI_KEY, '0');
      this.open.update((o) => ({ ...o, [key]: true }));
      return;
    }
    this.open.update((o) => ({ ...o, [key]: !o[key] }));
  }

  doSearch(): void {
    this.router.navigate(['/inventario/catalogo'], { queryParams: { q: this.search() || null } });
  }
}