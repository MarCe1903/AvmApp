import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface AuthResponse {
  token?: string;
  fullName: string;
  role: string;
  email?: string;
  requiresMfa?: boolean;
  mfaChallengeId?: string;
  mfaEnabled?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly storageKey = 'avmapp.auth';
  private readonly session$ = new BehaviorSubject<AuthResponse | null>(this.safeGetSession());

  constructor(private http: HttpClient) {}

  register(payload: { email: string; password: string; fullName: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(buildApiUrl('auth/register'), payload);
  }

  login(payload: { email: string; password: string }): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(buildApiUrl('auth/login'), payload);
  }

  verifyMfa(challengeId: string, code: string): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(buildApiUrl('auth/verify-mfa'), { challengeId, code });
  }

  toggleMfa(enabled: boolean): Observable<void> {
    return this.http.post<void>(buildApiUrl('auth/mfa/toggle'), { enabled });
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(buildApiUrl('auth/change-password'), {
      currentPassword,
      newPassword
    });
  }

  saveSession(response: AuthResponse): void {
    if (!this.hasStorage()) {
      return;
    }

    localStorage.setItem(this.storageKey, JSON.stringify(response));
    this.session$.next(response);
  }

  updateSession(patch: Partial<AuthResponse>): void {
    const current = this.getSession();
    if (!current) {
      return;
    }

    this.saveSession({ ...current, ...patch });
  }

  getSession(): AuthResponse | null {
    return this.session$.value;
  }

  clearSession(): void {
    if (this.hasStorage()) {
      localStorage.removeItem(this.storageKey);
    }

    this.session$.next(null);
  }

  getToken(): string | null {
    return this.getSession()?.token ?? null;
  }

  isAdmin(): boolean {
    return this.getSession()?.role === 'Admin';
  }

  isSeller(): boolean {
    return this.getSession()?.role === 'Seller';
  }

  isAdminOrSeller(): boolean {
    const role = this.getSession()?.role;
    return role === 'Admin' || role === 'Seller';
  }

  sessionChanges(): Observable<AuthResponse | null> {
    return this.session$.asObservable();
  }

  private hasStorage(): boolean {
    return typeof window !== 'undefined' && !!window.localStorage;
  }

  private safeGetSession(): AuthResponse | null {
    if (!this.hasStorage()) {
      return null;
    }

    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }

    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      return null;
    }
  }
}
