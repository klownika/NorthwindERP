import { Injectable, signal } from '@angular/core';

/**
 * Permite que una pantalla de detalle cambie el título de la franja
 * (por ejemplo "Pedido #10443" o el nombre del cliente).
 */
@Injectable({ providedIn: 'root' })
export class PageService {
  readonly titleOverride = signal<string | null>(null);
  readonly crumbOverride = signal<string | null>(null);
  /** Cambia cuando un panel de detalle modifica datos, para que la lista de atrás se recargue. */
  readonly dataVersion = signal(0);

  notifyDataChanged(): void {
    this.dataVersion.update((v) => v + 1);
  }

  setDetail(title: string): void {
    this.titleOverride.set(title);
    this.crumbOverride.set(title);
  }
  clear(): void {
    this.titleOverride.set(null);
    this.crumbOverride.set(null);
  }
}
