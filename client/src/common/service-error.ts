import { catchError } from 'rxjs';

export class ServiceError extends Error {
  readonly originalCause?: unknown;

  constructor(message: string, cause?: unknown) {
    super(message);
    this.name = 'ServiceError';
    this.originalCause = cause;
  }
}

export function wrapServiceError<T>(context: string) {
  return catchError<T, never>((err: unknown) => {
    throw new ServiceError(context, err);
  });
}
