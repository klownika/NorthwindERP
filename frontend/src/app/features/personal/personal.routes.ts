import { Routes } from '@angular/router';

/** Módulo Personal (carga diferida). "Mi cuenta" vive en /cuenta (se abre desde el avatar). */
export const PERSONAL_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'empleados' },
  {
    path: 'empleados', loadComponent: () => import('./empleados/empleados').then((m) => m.EmpleadosComponent),
    data: { module: 'personal', title: 'Empleados', subtitle: 'Personal de Northwind (solo lectura).', src: 'GET /api/employees · Employees' },
  },
  {
    path: 'transportistas', loadComponent: () => import('./transportistas/transportistas').then((m) => m.TransportistasComponent),
    data: { module: 'personal', title: 'Transportistas', subtitle: 'Empresas de transporte de los pedidos.', src: 'GET /api/shippers · Shippers' },
  },
];