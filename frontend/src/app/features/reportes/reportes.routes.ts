import { Routes } from '@angular/router';

/** Módulo Reportes (carga diferida). */
export const REPORTES_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'por-cliente' },
  {
    path: 'por-cliente', loadComponent: () => import('./por-cliente/por-cliente').then((m) => m.PorClienteComponent),
    data: { module: 'reportes', title: 'Ventas por cliente', subtitle: 'Ventas totales por cliente (solo clientes con al menos un pedido).', src: 'GET /api/reports/sales-by-customer · vw_sales_by_customer' },
  },
];
