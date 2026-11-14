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
    ],
  },
  { path: '**', redirectTo: '' },
];
