import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserInfo,
} from '../models/auth.model';

const ACCESS_TOKEN_KEY = 'ft_access_token';
const REFRESH_TOKEN_KEY = 'ft_refresh_token';
const USER_KEY = 'ft_user';
const EXPIRES_AT_KEY = 'ft_expires_at';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;

  private _user = signal<UserInfo | null>(this.loadUserFromStorage());

  currentUser = this._user.asReadonly();
  isLoggedIn = computed(() => !!this._user());
  isManager = computed(() =>
    ['Manager', 'Admin', 'SuperAdmin'].includes(this._user()?.role ?? ''),
  );
  isAdmin = computed(() =>
    ['Admin', 'SuperAdmin'].includes(this._user()?.role ?? ''),
  );

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, request)
      .pipe(tap((r) => this.saveSession(r)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(tap((r) => this.saveSession(r)));
  }

  refreshToken(): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh-token`, {
        accessToken: this.getAccessToken(),
        refreshToken: this.getRefreshToken(),
      })
      .pipe(tap((r) => this.saveSession(r)));
  }

  logout(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    localStorage.removeItem(REFRESH_TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    localStorage.removeItem(EXPIRES_AT_KEY);
    this._user.set(null);
    this.router.navigate(['/auth/login']);
  }

  getAccessToken(): string | null {
    return localStorage.getItem(ACCESS_TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(REFRESH_TOKEN_KEY);
  }

  /** Returns true if the access token exists and is not yet expired */
  isTokenValid(): boolean {
    const token = this.getAccessToken();
    const expiresAt = localStorage.getItem(EXPIRES_AT_KEY);
    if (!token || !expiresAt) return false;
    // Give a 30-second buffer so we refresh slightly before actual expiry
    return new Date(expiresAt).getTime() - 30_000 > Date.now();
  }

  /** True if we have a user in state but the token is expired — needs refresh */
  needsRefresh(): boolean {
    return !!this._user() && !!this.getRefreshToken() && !this.isTokenValid();
  }

  hasRole(...roles: string[]): boolean {
    return roles.includes(this._user()?.role ?? '');
  }

  private saveSession(response: AuthResponse): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, response.accessToken);
    localStorage.setItem(REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(USER_KEY, JSON.stringify(response.user));
    localStorage.setItem(EXPIRES_AT_KEY, response.expiresAt);
    this._user.set(response.user);
  }

  private loadUserFromStorage(): UserInfo | null {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? JSON.parse(raw) : null;
  }
}
