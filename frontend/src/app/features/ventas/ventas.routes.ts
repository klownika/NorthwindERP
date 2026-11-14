import { Routes } from '@angular/router';

/** Módulo Ventas (carga diferida). */
export const VENTAS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  {
    path: 'pedidos', loadComponent: () => import('./pedidos/pedidos').then((m) => m.PedidosComponent),
    data: { module: 'ventas', title: 'Pedidos', subtitle: 'Pedidos con su total. Abre uno para ver su detalle.', src: 'GET /api/orders · vw_order_details_extended' },
  },
];
