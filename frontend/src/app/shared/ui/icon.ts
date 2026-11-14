import { Component, input } from '@angular/core';

export type IconName =
  | 'home' | 'box' | 'bag' | 'users' | 'chart' | 'search' | 'plus' | 'alert' | 'logout'
  | 'back' | 'db' | 'check' | 'warehouse' | 'truck' | 'chevron';

/** Iconos de línea (stroke) en SVG. Uso: <app-icon name="box" [size]="20" /> */
@Component({
  selector: 'app-icon',
  template: `
    <svg aria-hidden="true" [attr.width]="size()" [attr.height]="size()" viewBox="0 0 24 24" fill="none" stroke="currentColor"
         [attr.stroke-width]="stroke()" stroke-linecap="round" stroke-linejoin="round">
      @switch (name()) {
        @case ('home') { <path d="M3 11l9-8 9 8"/><path d="M5 10v10h14V10"/> }
        @case ('box') { <path d="M21 8l-9-5-9 5 9 5 9-5z"/><path d="M3 8v8l9 5 9-5V8"/><path d="M12 13v8"/> }
        @case ('bag') { <path d="M6 2l-2 4v14a2 2 0 002 2h12a2 2 0 002-2V6l-2-4z"/><path d="M4 6h16"/><path d="M16 10a4 4 0 01-8 0"/> }
        @case ('users') { <circle cx="9" cy="8" r="4"/><path d="M2 21a7 7 0 0114 0"/><path d="M16 4a4 4 0 010 8"/><path d="M22 21a7 7 0 00-5-6.7"/> }
        @case ('chart') { <path d="M4 20V10"/><path d="M10 20V4"/><path d="M16 20v-7"/><path d="M22 20H2"/> }
        @case ('search') { <circle cx="11" cy="11" r="7"/><path d="M20 20l-3.5-3.5"/> }
        @case ('plus') { <path d="M12 5v14"/><path d="M5 12h14"/> }
        @case ('alert') { <path d="M10.3 3.9L1.8 18a2 2 0 001.7 3h17a2 2 0 001.7-3L13.7 3.9a2 2 0 00-3.4 0z"/><path d="M12 9v4"/><path d="M12 17h.01"/> }
        @case ('logout') { <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><path d="M16 17l5-5-5-5"/><path d="M21 12H9"/> }
        @case ('back') { <path d="M15 18l-6-6 6-6"/> }
        @case ('db') { <ellipse cx="12" cy="5" rx="8" ry="3"/><path d="M4 5v14c0 1.7 3.6 3 8 3s8-1.3 8-3V5"/><path d="M4 12c0 1.7 3.6 3 8 3s8-1.3 8-3"/> }
        @case ('check') { <path d="M20 6L9 17l-5-5"/> }
        @case ('warehouse') { <path d="M3 21V8l9-5 9 5v13"/><path d="M7 21v-8h10v8"/><path d="M7 17h10"/> }
        @case ('truck') { <path d="M1 6h13v10H1z"/><path d="M14 9h4l4 4v3h-8"/><circle cx="5.5" cy="18.5" r="2"/><circle cx="17.5" cy="18.5" r="2"/> }
        @case ('chevron') { <path d="M6 9l6 6 6-6"/> }
      }
    </svg>
  `,
  styles: `:host { display: inline-flex; flex: 0 0 auto; }`,
})
export class IconComponent {
  readonly name = input.required<IconName>();
  readonly size = input(20);
  readonly stroke = input(2);
}
