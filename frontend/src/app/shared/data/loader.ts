import { signal } from '@angular/core';
import { Observable } from 'rxjs';
import { UiError, toUiError } from '../../core/util/problem';

/**
 * Estado de carga de una pantalla.
 *   readonly load = new Loader();
 *   this.load.run(forkJoin({...}), (r) => this.data.set(r));
 */
export class Loader {
  readonly loading = signal(false);
  readonly error = signal<UiError | null>(null);

  run<T>(source: Observable<T>, onOk: (value: T) => void): void {
    this.loading.set(true);
    this.error.set(null);
    source.subscribe({
      next: (v) => { onOk(v); this.loading.set(false); },
      error: (e: unknown) => { this.error.set(toUiError(e)); this.loading.set(false); },
    });
  }
}
