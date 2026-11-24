import { Routes } from '@angular/router';

/** Módulo Ventas (carga diferida). El detalle de pedidos y clientes se abre en un panel sobre su lista. */
export const VENTAS_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'pedidos' },
  {
    path: 'pedidos', loadComponent: () => import('./pedidos/pedidos').then((m) => m.PedidosComponent),
    data: { module: 'ventas', title: 'Pedidos', subtitle: 'Pedidos con su total. Abre uno para ver su detalle.' },
    children: [
      {
        path: ':id', loadComponent: () => import('./pedido-detalle/pedido-detalle').then((m) => m.PedidoDetalleComponent),
        data: { module: 'ventas', title: 'Pedido', subtitle: 'Detalle del pedido con sus productos y el total por línea.' },
      },
    ],
  },
  {
    path: 'nuevo-pedido', loadComponent: () => import('./nuevo-pedido/nuevo-pedido').then((m) => m.NuevoPedidoComponent),
    data: { module: 'ventas', title: 'Nuevo pedido', subtitle: 'Registra un pedido completo: cliente, almacén y productos en una sola transacción.' },
  },
  {
    path: 'clientes', loadComponent: () => import('./clientes/clientes').then((m) => m.ClientesComponent),
    data: { module: 'ventas', title: 'Clientes', subtitle: 'Clientes de Northwind. Abre una ficha para editar su dirección o ver su historial.' },
    children: [
      {
        path: ':id', loadComponent: () => import('./cliente-ficha/cliente-ficha').then((m) => m.ClienteFichaComponent),
        data: { module: 'ventas', title: 'Cliente', subtitle: 'Ficha del cliente, edición de dirección e historial.' },
      },
    ],
  },
];
