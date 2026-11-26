import { DestroyRef, Directive, ElementRef, inject, output } from '@angular/core';

/**
 * Avisa cuántas columnas tiene de verdad una grilla CSS (auto-fill/auto-fit) y vuelve a avisar al
 * redimensionarla. Uso: <div class="grid" (gridCols)="pager.resize(fullRows($event))">
 */
@Directive({ selector: '[gridCols]' })
export class GridColsDirective {
  readonly gridCols = output<number>();

  constructor() {
    const el = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
    let last = 0;
    const ro = new ResizeObserver(() => {
      const cols = getComputedStyle(el).gridTemplateColumns.split(' ').filter((t) => t && t !== 'none').length || 1;
      if (cols !== last) { last = cols; this.gridCols.emit(cols); }
    });
    ro.observe(el);
    inject(DestroyRef).onDestroy(() => ro.disconnect());
  }
}
