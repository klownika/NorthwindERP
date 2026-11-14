import { Routes } from '@angular/router';

/** Módulo Personal (carga diferida). Mi */
export const PERSONAL_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'empleados' },
];
