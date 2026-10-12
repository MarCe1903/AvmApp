import { Component, OnDestroy, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService, AuthResponse } from './services/auth.service';
import { Subscription } from 'rxjs';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="app-header">
      <div class="brand">
        <span class="dot"></span>
        <a routerLink="/">AVM App</a>
      </div>

      <nav class="nav-links">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Ana Sayfa</a>
        <a routerLink="/profile" routerLinkActive="active" *ngIf="isAuthenticated">Profil</a>
        <a routerLink="/orders" routerLinkActive="active" *ngIf="isAuthenticated">Siparişlerim</a>
        <a routerLink="/admin" routerLinkActive="active" *ngIf="isAdmin">Yönetim</a>
        <a routerLink="/seller" routerLinkActive="active" *ngIf="isAdmin || isSeller">Satıcı paneli</a>
      </nav>

      <div class="session-chip" *ngIf="isAuthenticated; else guest">
        <div class="avatar">{{ userInitials }}</div>
        <div class="session-meta">
          <span class="eyebrow">{{ isAdmin ? 'Admin' : (isSeller ? 'Satıcı' : 'Üye') }}</span>
          <strong>{{ userName }}</strong>
        </div>
        <button class="ghost-btn" type="button" (click)="logout()">Çıkış</button>
      </div>
      <ng-template #guest>
        <span class="muted-text">Giriş/kayıt ana sayfada</span>
      </ng-template>
    </header>

    <main class="app-shell">
      <router-outlet></router-outlet>
    </main>
  `,
  styleUrls: ['./app.css']
})
export class AppComponent implements OnInit, OnDestroy, AfterViewInit {
  isAuthenticated = false;
  isAdmin = false;
  isSeller = false;
  userName: string | null = null;
  private sub?: Subscription;

  constructor(private auth: AuthService, private router: Router, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.captureOAuthQuerySession();
    this.applySession(this.auth.getSession());
  }

  ngAfterViewInit(): void {
    this.sub = this.auth.sessionChanges().subscribe((session) => {
      this.applySession(session);
      this.cdr.detectChanges();
    });
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  get userInitials(): string {
    if (!this.userName) return 'AD';
    return this.userName
      .split(' ')
      .filter(Boolean)
      .map((p) => p[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  logout(): void {
    this.auth.clearSession();
    this.router.navigateByUrl('/');
  }
  private captureOAuthQuerySession(): void {
    if (typeof window === "undefined") return;

    const url = new URL(window.location.href);
    const token = url.searchParams.get("token");
    const fullName = url.searchParams.get("fullName");
    const role = url.searchParams.get("role");
    const email = url.searchParams.get("email");

    if (!token || !fullName || !role) {
      return;
    }

    this.auth.saveSession({ token, fullName, role, email: email ?? undefined });
    url.searchParams.delete("token");
    url.searchParams.delete("fullName");
    url.searchParams.delete("role");
    url.searchParams.delete("email");
    window.history.replaceState({}, "", ``);
  }

  private applySession(session: AuthResponse | null): void {
    this.isAuthenticated = !!session;
    this.isAdmin = session?.role === 'Admin';
    this.isSeller = session?.role === 'Seller';
    this.userName = session?.fullName ?? null;
  }
}
