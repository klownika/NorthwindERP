import { Routes } from '@angular/router';

/** Módulo Inventario (carga diferida). */
export const INVENTARIO_ROUTES: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'catalogo' },
  {
    path: 'catalogo', loadComponent: () => import('./catalogo/catalogo').then((m) => m.CatalogoComponent),
    data: { module: 'inventario', title: 'Catálogo', subtitle: 'Productos con su categoría, proveedor, precio y stock.', src: 'GET /api/products · GET /api/categories · Products + Categories + Suppliers' },
  },
  {
    path: 'stock', loadComponent: () => import('./stock/stock').then((m) => m.StockComponent),
    data: { module: 'inventario', title: 'Stock', subtitle: 'Stock por almacén, mínimo de reposición y verificación contra el Kardex.', src: 'GET /api/inventory/stock · vw_stock_status' },
  },
  {
    path: 'kardex', loadComponent: () => import('./kardex/kardex').then((m) => m.KardexComponent),
    data: { module: 'inventario', title: 'Kardex', subtitle: 'Historial de movimientos: cada cambio de stock deja su fila.', src: 'GET /api/inventory/kardex · InventoryTransactions' },
  },
  {
    path: 'almacenes', loadComponent: () => import('./almacenes/almacenes').then((m) => m.AlmacenesComponent),
    data: { module: 'inventario', title: 'Almacenes', subtitle: 'Almacenes, su responsable y el estado de su stock.', src: 'GET /api/inventory/warehouses · Warehouses + Employees' },
  },
];
