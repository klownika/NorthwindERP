import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';

/**
 * Rutas de StockIN.
 *  /login                 → pantalla de login
 *  /                      → Inicio (dentro del shell: sidebar + header + franja)
 *  /inventario/...        → módulo Inventario   (carga diferida)
 *  /ventas/...            → módulo Ventas       (carga diferida)
 *  /personal/...          → módulo Personal     (carga diferida)
 *  /reportes/...          → módulo Reportes     (carga diferida)
 *  /cuenta                → Mi cuenta (se abre desde el avatar)
 */
export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login').then((m) => m.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.ShellComponent),
    children: [
      {
        path: '', pathMatch: 'full',
        loadComponent: () => import('./features/inicio/inicio').then((m) => m.InicioComponent),
        data: { title: 'Inicio', subtitle: 'Resumen de ventas, pedidos e inventario.', src: 'GET /api/orders · GET /api/reports/sales-by-customer · vw_stock_status' },
      },
      { path: 'inventario', loadChildren: () => import('./features/inventario/inventario.routes').then((m) => m.INVENTARIO_ROUTES) },
      { path: 'ventas', loadChildren: () => import('./features/ventas/ventas.routes').then((m) => m.VENTAS_ROUTES) },
      { path: 'personal', loadChildren: () => import('./features/personal/personal.routes').then((m) => m.PERSONAL_ROUTES) },
      {
        path: 'cuenta',
        loadComponent: () => import('./features/personal/mi-cuenta/mi-cuenta').then((m) => m.MiCuentaComponent),
        data: { title: 'Mi cuenta', subtitle: 'Tu sesión y tu contraseña.', src: 'GET /api/auth/me · POST /api/auth/change-password · UserAccounts', accountIcon: true },
      },
      { path: 'reportes', loadChildren: () => import('./features/reportes/reportes.routes').then((m) => m.REPORTES_ROUTES) },
    ],
  },
  { path: '**', redirectTo: '' },
];
