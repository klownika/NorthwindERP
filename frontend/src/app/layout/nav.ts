import { IconName } from '../shared/ui/icon';

/** Estructura del sidebar: módulos y submódulos (cada submódulo es una ruta). */
export interface NavSub { label: string; path: string; }
export interface NavModule { key: string; label: string; icon: IconName; base: string; subs: NavSub[]; }

export const NAV: NavModule[] = [
  {
    key: 'inventario', label: 'Inventario', icon: 'box', base: '/inventario',
    subs: [
      { label: 'Catálogo', path: '/inventario/catalogo' },
      { label: 'Stock', path: '/inventario/stock' },
      { label: 'Kardex', path: '/inventario/kardex' },
      { label: 'Almacenes', path: '/inventario/almacenes' },
      { label: 'Proveedores', path: '/inventario/proveedores' },
    ],
  },
  {
    key: 'ventas', label: 'Ventas', icon: 'bag', base: '/ventas',
    subs: [
      { label: 'Pedidos', path: '/ventas/pedidos' },
      { label: 'Nuevo pedido', path: '/ventas/nuevo-pedido' },
      { label: 'Clientes', path: '/ventas/clientes' },
    ],
  },
  {
    key: 'personal', label: 'Personal', icon: 'users', base: '/personal',
    subs: [
      { label: 'Empleados', path: '/personal/empleados' },
      { label: 'Transportistas', path: '/personal/transportistas' },
    ],
  },
  {
    key: 'reportes', label: 'Reportes', icon: 'chart', base: '/reportes',
    subs: [
      { label: 'Ventas por cliente', path: '/reportes/por-cliente' },
      { label: 'Ventas por país', path: '/reportes/por-pais' },
      { label: 'Por año · clientes', path: '/reportes/anio-clientes' },
      { label: 'Por año · empleados', path: '/reportes/anio-empleados' },
    ],
  },
];

/** Datos que cada ruta declara en `data` para pintar la franja (hero). */
export interface PageData {
  title: string;
  subtitle: string;
  /** Endpoint y objeto de la BD que alimentan la pantalla. */
  src: string;
  /** Clave del módulo (para pestañas y migas). Vacío en Inicio y Mi cuenta. */
  module?: string;
  /** Muestra el icono glossy de persona (Mi cuenta). */
  accountIcon?: boolean;
}
