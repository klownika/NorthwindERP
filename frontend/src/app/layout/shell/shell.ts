import { Component, computed, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { NavigationEnd, NavigationStart, Router, RouterLink, RouterOutlet } from '@angular/router';
import { filter, map, startWith } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { GlossyIconComponent, GlossyIconName } from '../../shared/ui/glossy-icon';
import { IconComponent } from '../../shared/ui/icon';
import { ToastOutletComponent } from '../../shared/ui/toast';
import { LowStockService } from '../low-stock.service';
import { NotificationsComponent } from '../notifications/notifications';
import { NAV, NavModule, PageData } from '../nav';
import { PageService } from '../page.service';

const MINI_KEY = 'stockin.sidebar.mini';

/** Estructura fija de la app: sidebar + header + franja DORfic + contenido de cada módulo. */
@Component({
  selector: 'app-shell',
  imports: [RouterOutlet, RouterLink, IconComponent, GlossyIconComponent, NotificationsComponent, ToastOutletComponent],
  templateUrl: './shell.html',
  styleUrl: './shell.scss',
  host: { '(document:keydown.escape)': 'menuOpen.set(false)' },
})
export class ShellComponent {
  private router = inject(Router);
  readonly auth = inject(AuthService);
  readonly page = inject(PageService);
  readonly lowStock = inject(LowStockService);

  readonly nav = NAV;
  readonly mini = signal(localStorage.getItem(MINI_KEY) === '1');
  readonly menuOpen = signal(false);
  readonly open = signal<Record<string, boolean>>({});
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
  readonly headIcon = computed<GlossyIconName>(() => (this.data().accountIcon ? 'users' : this.currentModule()?.glossy ?? 'home'));

  readonly me = computed(() => {
    const s = this.auth.session();
    const name = s?.username ?? '';
    return { name, role: s?.role ?? '', initials: name.slice(0, 2).toUpperCase() };
  });

  constructor() {
    this.lowStock.refresh();
    // Al salir de una pantalla se limpia el título de detalle (lo vuelve a poner la siguiente si lo necesita).
    this.router.events.pipe(filter((e) => e instanceof NavigationStart)).subscribe(() => this.page.clear());
    // Abre el módulo de la pantalla actual (sin cerrar los que abrió el usuario) y cierra el menú móvil al navegar.
    this.openCurrentModule();
    this.router.events.pipe(filter((e) => e instanceof NavigationEnd)).subscribe(() => {
      this.openCurrentModule();
      this.menuOpen.set(false);
    });
  }

  private openCurrentModule(): void {
    const m = this.currentModule();
    if (m && !this.open()[m.key]) this.open.update((o) => ({ ...o, [m.key]: true }));
  }

  private isMobile(): boolean { return window.matchMedia('(max-width: 899px)').matches; }

  isActive(path: string): boolean { return this.url().startsWith(path); }

  toggleMini(): void {
    if (this.isMobile()) { this.menuOpen.set(false); return; }
    this.mini.update((v) => !v);
    localStorage.setItem(MINI_KEY, this.mini() ? '1' : '0');
  }

  toggleModule(key: string): void {
    if (this.mini() && !this.isMobile()) {
      this.mini.set(false);
      localStorage.setItem(MINI_KEY, '0');
      this.open.update((o) => ({ ...o, [key]: true }));
      return;
    }
    // Cada módulo se abre y se cierra por su cuenta, sin afectar a los demás.
    this.open.update((o) => ({ ...o, [key]: !o[key] }));
  }

  doSearch(): void {
    this.router.navigate(['/inventario/catalogo'], { queryParams: { q: this.search() || null } });
  }
}