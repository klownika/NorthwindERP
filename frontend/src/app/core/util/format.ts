/** Formatos usados en toda la app. */
export const money = (n: number | null | undefined): string =>
  '$ ' + Number(n ?? 0).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

export const int = (n: number | null | undefined): string => Number(n ?? 0).toLocaleString('en-US');

/** '1997-02-12T00:00:00' -> '12/02/1997' */
export const fmtDate = (iso: string | null | undefined): string => {
  if (!iso) return '—';
  const [y, m, d] = iso.slice(0, 10).split('-');
  return `${d}/${m}/${y}`;
};

/** '2026-11-13T10:30:00' -> '13/11/2026 10:30' */
export const fmtDateTime = (iso: string | null | undefined): string =>
  iso ? `${fmtDate(iso)} ${iso.slice(11, 16)}` : '—';

export const pct = (part: number, total: number): string => (total ? (part / total) * 100 : 0).toFixed(1) + '%';

export const norm = (s: string | null | undefined): string => (s ?? '').trim().toLowerCase();
