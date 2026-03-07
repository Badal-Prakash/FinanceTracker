import { inject } from '@angular/core';
import { CanActivateFn, Router, ActivatedRouteSnapshot } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/auth.service';

// ── Auth Guard — redirect to login if not authenticated ───────────────────────
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Not logged in at all
  if (!auth.isLoggedIn()) {
    router.navigate(['/auth/login']);
    return false;
  }

  // Token still valid — allow through immediately
  if (auth.isTokenValid()) return true;

  // Token expired but we have a refresh token — attempt silent refresh
  if (auth.needsRefresh()) {
    return auth.refreshToken().pipe(
      map(() => true),
      catchError(() => {
        auth.logout();
        return of(false);
      }),
    );
  }

  // No valid token and no refresh token — logout
  auth.logout();
  return false;
};

// ── Guest Guard — redirect to home if already logged in ──────────────────────
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (!auth.isLoggedIn()) return true;

  router.navigate(['/']);
  return false;
};

// ── Role Guard — redirect to 403 if user lacks required role ─────────────────
// Usage in routes:  canActivate: [roleGuard('Admin', 'SuperAdmin')]
export const roleGuard =
  (...roles: string[]): CanActivateFn =>
  (route: ActivatedRouteSnapshot) => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (auth.hasRole(...roles)) return true;

    router.navigate(['/forbidden']);
    return false;
  };
