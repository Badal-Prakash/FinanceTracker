import {
  HttpInterceptorFn,
  HttpErrorResponse,
  HttpRequest,
  HttpHandlerFn,
} from '@angular/common/http';
import { inject } from '@angular/core';
import {
  BehaviorSubject,
  catchError,
  filter,
  switchMap,
  take,
  throwError,
  Observable,
} from 'rxjs';
import { AuthService } from '../services/auth.service';

let isRefreshing = false;
const refreshDone$ = new BehaviorSubject<string | null>(null);

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);

  // Skip auth header for auth endpoints themselves
  if (req.url.includes('/auth/')) return next(req);

  return next(addToken(req, auth.getAccessToken())).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401) return throwError(() => error);
      return handle401(req, next, auth, error);
    }),
  );
};

function addToken(
  req: HttpRequest<unknown>,
  token: string | null,
): HttpRequest<unknown> {
  return token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;
}

function handle401(
  req: HttpRequest<unknown>,
  next: HttpHandlerFn,
  auth: AuthService,
  originalError: HttpErrorResponse,
): Observable<any> {
  if (!auth.getRefreshToken()) {
    auth.logout();
    return throwError(() => originalError);
  }

  // If a refresh is already in flight — wait for it, then retry with new token
  if (isRefreshing) {
    return refreshDone$.pipe(
      filter((token) => token !== null),
      take(1),
      switchMap((token) => next(addToken(req, token))),
    );
  }

  // Start the refresh
  isRefreshing = true;
  refreshDone$.next(null);

  return auth.refreshToken().pipe(
    switchMap((response) => {
      isRefreshing = false;
      refreshDone$.next(response.accessToken);
      return next(addToken(req, response.accessToken));
    }),
    catchError((err) => {
      isRefreshing = false;
      refreshDone$.next(null);
      auth.logout();
      return throwError(() => err);
    }),
  );
}
