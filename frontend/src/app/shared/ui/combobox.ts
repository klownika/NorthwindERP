import { Component, ElementRef, OnDestroy, computed, forwardRef, input, signal, viewChild } from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { IconComponent } from './icon';

/** Opción del combobox: texto principal y, opcionalmente, uno secundario (por ejemplo el país). */
export interface ComboOption<T = unknown> { value: T; label: string; hint?: string | null; }

/** Minúsculas y sin tildes: "Océano" -> "oceano". */
const fold = (s: string): string => s.normalize('NFD').replace(/[̀-ͯ]/g, '').toLowerCase().trim();

let nextId = 0;

/**
 * Lista desplegable con búsqueda. Reemplaza al <select> nativo y funciona con ngModel:
 *   <app-combobox inputId="cw" [options]="opts()" emptyLabel="Todos los almacenes" [emptyValue]="0"
 *                 [ngModel]="wh()" (ngModelChange)="wh.set($event)" />
 * La lista se abre en la capa superior (popover), así que no la recortan modales ni paneles.
 */
@Component({
  selector: 'app-combobox',
  imports: [IconComponent],
  providers: [{ provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => ComboboxComponent), multi: true }],
  host: { '(document:pointerdown)': 'onDocPointer($event)' },
  template: `
    <div class="box">
      <input #inp class="field" type="text" role="combobox" autocomplete="off" spellcheck="false"
             [attr.id]="inputId()" [attr.aria-label]="ariaLabel()" [attr.aria-expanded]="open()" [attr.aria-controls]="listId"
             aria-autocomplete="list" [attr.aria-activedescendant]="open() && filtered()[active()] ? listId + '-' + active() : null"
             [placeholder]="placeholder()" [disabled]="isDisabled()" [value]="text()"
             (focus)="openList()" (click)="openList()" (input)="onInput($any($event.target).value)" (keydown)="onKey($event)" (blur)="onBlur()" />
      <button type="button" class="chev" tabindex="-1" aria-hidden="true" [disabled]="isDisabled()" (mousedown)="$event.preventDefault()" (click)="toggle()">
        <app-icon name="chevron" [size]="16" [stroke]="2.4" />
      </button>
    </div>
    <ul #list popover="manual" role="listbox" class="list" [id]="listId" [attr.aria-label]="ariaLabel() || placeholder() || 'Opciones'"
        [style.top.px]="pos().top" [style.bottom.px]="pos().bottom" [style.left.px]="pos().left" [style.width.px]="pos().width"
        [style.max-height.px]="pos().maxHeight" (mousedown)="$event.preventDefault()">
      @for (o of filtered(); track $index; let i = $index) {
        <li role="option" class="opt" [id]="listId + '-' + i" [class.act]="i === active()" [attr.aria-selected]="o.value === value()"
            (mousedown)="$event.preventDefault()" (mousemove)="active.set(i)" (click)="choose(o)">
          <span class="main">{{ o.label }}</span>
          @if (o.hint) { <span class="hint">{{ o.hint }}</span> }
        </li>
      } @empty {
        <li class="none" role="presentation">Sin resultados</li>
      }
    </ul>
  `,
  styles: `
    :host { display: block; min-width: 0; }
    .box { position: relative; }
    .field { padding-right: 40px; text-overflow: ellipsis; cursor: pointer; }
    .field:focus { cursor: text; }
    .field:disabled { background: #faf5f0; color: var(--ink-4); cursor: not-allowed; }
    .chev {
      position: absolute; top: 0; right: 0; width: 40px; height: 44px; display: inline-flex; align-items: center; justify-content: center;
      border: 0; background: transparent; color: var(--or-700); cursor: pointer;
    }
    .chev:disabled { color: var(--ink-4); cursor: not-allowed; }
    [aria-expanded='true'] + .chev { transform: rotate(180deg); }
    .list {
      position: fixed; inset: auto; margin: 0; padding: 6px; overflow-y: auto; list-style: none;
      background: #fff; border: 1px solid #e9c8ae; border-radius: 12px; color: var(--ink);
      box-shadow: 0 16px 32px -16px rgba(124, 45, 18, .45);
    }
    .opt { display: flex; align-items: baseline; gap: 8px; min-height: 40px; padding: 9px 10px; border-radius: 8px; font-size: 14px; cursor: pointer; }
    .opt.act { background: #fff1e4; }
    .opt[aria-selected='true'] { color: var(--or-800); font-weight: 700; }
    .main { min-width: 0; overflow-wrap: anywhere; }
    .hint { margin-left: auto; flex: 0 0 auto; font-size: 12px; font-weight: 500; color: var(--ink-4); }
    .none { padding: 10px; font-size: 14px; color: var(--ink-3); }
  `,
})
export class ComboboxComponent implements ControlValueAccessor, OnDestroy {
  private inp = viewChild.required<ElementRef<HTMLInputElement>>('inp');
  private list = viewChild.required<ElementRef<HTMLElement>>('list');

  readonly listId = `combo-list-${nextId++}`;
  readonly options = input<ComboOption[]>([]);
  readonly inputId = input<string | null>(null);
  readonly placeholder = input('');
  readonly ariaLabel = input<string | null>(null);
  readonly disabled = input(false);
  /** Opción "Todos" / vacía que se muestra al inicio de la lista. */
  readonly emptyLabel = input<string | null>(null);
  readonly emptyValue = input<unknown>(null);

  readonly value = signal<unknown>(null);
  readonly open = signal(false);
  readonly query = signal('');
  readonly typing = signal(false);
  readonly active = signal(0);
  readonly pos = signal({ top: null as number | null, bottom: null as number | null, left: 0, width: 0, maxHeight: 280 });
  private readonly formDisabled = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.formDisabled());

  readonly all = computed<ComboOption[]>(() => {
    const empty = this.emptyLabel();
    return empty === null ? this.options() : [{ value: this.emptyValue(), label: empty }, ...this.options()];
  });
  readonly filtered = computed(() => {
    const q = fold(this.query());
    if (!this.typing() || !q) return this.all();
    return this.all().filter((o) => fold(`${o.label} ${o.hint ?? ''}`).includes(q));
  });
  readonly selected = computed(() => this.all().find((o) => o.value === this.value()) ?? null);
  readonly text = computed(() => {
    if (this.typing()) return this.query();
    const s = this.selected();
    return s ? (s.hint ? `${s.label} · ${s.hint}` : s.label) : '';
  });

  private onChange: (v: unknown) => void = () => {};
  private onTouched: () => void = () => {};
  private readonly reposition = () => this.place();

  writeValue(v: unknown): void { this.value.set(v); }
  registerOnChange(fn: (v: unknown) => void): void { this.onChange = fn; }
  registerOnTouched(fn: () => void): void { this.onTouched = fn; }
  setDisabledState(d: boolean): void { this.formDisabled.set(d); }

  ngOnDestroy(): void { this.detach(); }

  openList(): void {
    if (this.open() || this.isDisabled()) return;
    this.typing.set(false);
    this.query.set('');
    this.active.set(Math.max(0, this.all().findIndex((o) => o.value === this.value())));
    this.open.set(true);
    this.place();
    const el = this.list().nativeElement;
    if (!el.matches(':popover-open')) el.showPopover();
    window.addEventListener('scroll', this.reposition, true);
    window.addEventListener('resize', this.reposition);
    setTimeout(() => this.scrollToActive());
    this.inp().nativeElement.select();
  }

  close(): void {
    if (!this.open()) return;
    this.open.set(false);
    this.typing.set(false);
    this.detach();
    this.onTouched();
  }

  toggle(): void {
    if (this.open()) { this.close(); return; }
    this.inp().nativeElement.focus();
    this.openList();
  }

  choose(o: ComboOption): void {
    if (o.value !== this.value()) {
      this.value.set(o.value);
      this.onChange(o.value);
    }
    this.close();
  }

  onInput(v: string): void {
    if (!this.open()) this.openList();
    this.query.set(v);
    this.typing.set(true);
    this.active.set(0);
  }

  onBlur(): void { this.close(); }

  onKey(e: KeyboardEvent): void {
    const n = this.filtered().length;
    switch (e.key) {
      case 'ArrowDown':
      case 'ArrowUp':
        e.preventDefault();
        if (!this.open()) { this.openList(); return; }
        if (n) this.active.set((this.active() + (e.key === 'ArrowDown' ? 1 : n - 1)) % n);
        this.scrollToActive();
        break;
      case 'Home': case 'End':
        if (this.open() && n) { e.preventDefault(); this.active.set(e.key === 'Home' ? 0 : n - 1); this.scrollToActive(); }
        break;
      case 'Enter':
        if (this.open()) {
          e.preventDefault();
          const o = this.filtered()[this.active()];
          if (o) this.choose(o);
        }
        break;
      case 'Escape':
        if (this.open()) { e.preventDefault(); e.stopPropagation(); this.close(); }
        break;
      case 'Tab':
        this.close();
        break;
    }
  }

  onDocPointer(e: PointerEvent): void {
    if (!this.open()) return;
    const t = e.target as Node;
    if (!this.inp().nativeElement.parentElement?.contains(t) && !this.list().nativeElement.contains(t)) this.close();
  }

  /** Ubica la lista bajo el campo (o encima si abajo no hay espacio). */
  private place(): void {
    const r = this.inp().nativeElement.getBoundingClientRect();
    const below = window.innerHeight - r.bottom - 12;
    const above = r.top - 12;
    const up = below < 200 && above > below;
    this.pos.set({
      top: up ? null : r.bottom + 4,
      bottom: up ? window.innerHeight - r.top + 4 : null,
      left: r.left, width: r.width,
      maxHeight: Math.max(120, Math.min(280, up ? above : below)),
    });
  }

  private scrollToActive(): void {
    this.list().nativeElement.querySelector<HTMLElement>(`#${this.listId}-${this.active()}`)?.scrollIntoView({ block: 'nearest' });
  }

  private detach(): void {
    window.removeEventListener('scroll', this.reposition, true);
    window.removeEventListener('resize', this.reposition);
    const el = this.list()?.nativeElement;
    if (el?.matches(':popover-open')) el.hidePopover();
  }
}
