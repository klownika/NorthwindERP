import { Routes } from '@angular/router';

/** Módulo Ventas (carga diferida). */
export const VENTAS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  {
    path: 'pedidos', loadComponent: () => import('./pedidos/pedidos').then((m) => m.PedidosComponent),
    data: { module: 'ventas', title: 'Pedidos', subtitle: 'Pedidos con su total. Abre uno para ver su detalle.', src: 'GET /api/orders · vw_order_details_extended' },
  },
  {
    path: 'pedidos/:id', loadComponent: () => import('./pedido-detalle/pedido-detalle').then((m) => m.PedidoDetalleComponent),
    data: { module: 'ventas', title: 'Pedido', subtitle: 'Detalle del pedido con sus productos y el total por línea.', src: 'GET /api/orders/{id} · vw_order_details_extended · DELETE → sp_delete_order' },
  },
  {
    path: 'nuevo-pedido', loadComponent: () => import('./nuevo-pedido/nuevo-pedido').then((m) => m.NuevoPedidoComponent),
    data: { module: 'ventas', title: 'Nuevo pedido', subtitle: 'Registra un pedido completo: cliente, almacén y productos en una sola transacción.', src: 'POST /api/orders/with-customer · INSERT Customers + CALL sp_register_order' },
  },
  {
    path: 'clientes', loadComponent: () => import('./clientes/clientes').then((m) => m.ClientesComponent),
    data: { module: 'ventas', title: 'Clientes', subtitle: 'Clientes de Northwind. Abre una ficha para editar su dirección o ver su historial.', src: 'GET /api/customers · Customers' },
  },
];
