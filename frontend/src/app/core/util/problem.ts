import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from '../models/api.models';

/** Mensaje de error listo para mostrar en pantalla. */
export interface UiError { title: string; messages: string[]; }

/**
 * Convierte la respuesta de error del backend (ProblemDetails / ValidationProblemDetails)
 * en un título "400 BAD REQUEST" y una lista de mensajes.
 */
export function toUiError(err: unknown): UiError {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) {
      return { title: 'SIN CONEXIÓN', messages: ['No se pudo contactar con la API. ¿Está corriendo el backend en http://localhost:5080?'] };
    }
    const body = (err.error ?? {}) as ProblemDetails;
    const messages: string[] = [];
    if (body.errors) Object.values(body.errors).forEach((list) => messages.push(...list));
    if (body.detail) messages.push(body.detail);
    if (!messages.length) messages.push(body.title ?? err.message);
    return { title: `${err.status} ${(err.statusText || body.title || '').toUpperCase()}`.trim(), messages: [...new Set(messages)] };
  }
  if (err instanceof Error) return { title: 'ERROR', messages: [err.message] };
  return { title: 'ERROR', messages: [String(err)] };
}
