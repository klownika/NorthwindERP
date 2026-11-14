import { Injectable, signal } from '@angular/core';

/**
 * Permite que una pantalla de detalle cambie el título de la franja
 * (por ejemplo "Pedido #10443" o el nombre del cliente).
 */
@Injectable({ providedIn: 'root' })
export class PageService {
  readonly titleOverride = signal<string | null>(null);
  readonly crumbOverride = signal<string | null>(null);

  setDetail(title: string): void {
    this.titleOverride.set(title);
    this.crumbOverride.set(title);
  }
  clear(): void {
    this.titleOverride.set(null);
    this.crumbOverride.set(null);
  }
}
