import { Routes } from '@angular/router';

/** Módulo Reportes (carga diferida). */
export const REPORTES_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'por-cliente' },
  {
    path: 'por-cliente', loadComponent: () => import('./por-cliente/por-cliente').then((m) => m.PorClienteComponent),
    data: { module: 'reportes', title: 'Ventas por cliente', subtitle: 'Ventas totales por cliente (solo clientes con al menos un pedido).' },
  },
  {
    path: 'por-pais', loadComponent: () => import('./por-pais/por-pais').then((m) => m.PorPaisComponent),
    data: { module: 'reportes', title: 'Ventas por país', subtitle: 'Ventas totales por país del cliente.' },
  },
  {
    path: 'anio-clientes', loadComponent: () => import('./anio-clientes/anio-clientes').then((m) => m.AnioClientesComponent),
    data: { module: 'reportes', title: 'Por año · clientes', subtitle: 'Pedidos, ventas y promedio por línea de cada cliente en el año.' },
  },
  {
    path: 'anio-empleados', loadComponent: () => import('./anio-empleados/anio-empleados').then((m) => m.AnioEmpleadosComponent),
    data: { module: 'reportes', title: 'Por año · empleados', subtitle: 'Pedidos, ventas y ticket promedio de cada vendedor en el año.' },
  },
];
