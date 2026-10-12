import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthResponse, AuthService } from '../../services/auth.service';
import { buildApiUrl } from '../../utils/api-url';

type AuthTab = 'login' | 'register';

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './auth-page.component.html',
  styleUrls: ['./auth-page.component.css']
})
export class AuthPageComponent implements OnInit {
  activeTab: AuthTab = 'login';
  returnUrl = '/';

  loginEmail = '';
  loginPassword = '';
  loginLoading = false;
  loginError: string | null = null;

  registerName = '';
  registerEmail = '';
  registerPassword = '';
  registerLoading = false;
  registerError: string | null = null;

  awaitingMfa = false;
  pendingMfaChallengeId: string | null = null;
  pendingMfaEmail = '';
  mfaCode = '';
  mfaLoading = false;
  mfaError: string | null = null;

  constructor(
    private authService: AuthService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    const session = this.authService.getSession();
    if (session?.token) {
      this.router.navigateByUrl('/');
      return;
    }

    this.route.queryParamMap.subscribe((params) => {
      this.activeTab = params.get('tab') === 'register' ? 'register' : 'login';
      this.returnUrl = params.get('returnUrl') || '/';
    });
  }

  selectTab(tab: AuthTab): void {
    this.activeTab = tab;
    this.resetMfaState();
    this.resetFeedback();

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { tab },
      queryParamsHandling: 'merge'
    });
  }

  openSocialAuth(provider: 'google' | 'facebook'): void {
    if (provider === 'google') {
      window.location.href = buildApiUrl('auth/google');
      return;
    }

    window.alert('Facebook ile giris yakinda.');
  }

  submitLogin(): void {
    if (this.loginLoading) {
      return;
    }

    this.loginError = null;
    if (!this.loginEmail.trim() || !this.loginPassword.trim()) {
      this.loginError = 'Lutfen e-posta ve sifre alanlarini doldurun.';
      return;
    }

    this.loginLoading = true;
    this.authService
      .login({ email: this.loginEmail.trim(), password: this.loginPassword })
      .subscribe({
        next: (response) => {
          this.loginLoading = false;

          if (response.requiresMfa && response.mfaChallengeId) {
            this.activateMfaStep(response.mfaChallengeId, this.loginEmail.trim());
            return;
          }

          this.completeAuth(response);
        },
        error: (err) => {
          this.loginLoading = false;
          this.loginError = err?.error?.message ?? 'Giris basarisiz. Bilgileri kontrol edin.';
        }
      });
  }

  submitRegister(): void {
    if (this.registerLoading) {
      return;
    }

    this.registerError = null;
    if (!this.registerName.trim() || !this.registerEmail.trim() || !this.registerPassword.trim()) {
      this.registerError = 'Tum alanlari doldurmaniz gerekiyor.';
      return;
    }

    this.registerLoading = true;
    this.authService
      .register({
        email: this.registerEmail.trim(),
        password: this.registerPassword,
        fullName: this.registerName.trim()
      })
      .subscribe({
        next: (response) => {
          this.registerLoading = false;
          this.completeAuth(response);
        },
        error: (err) => {
          this.registerLoading = false;
          this.registerError = err?.error?.message ?? 'Kayit basarisiz. Bilgileri kontrol edin.';
        }
      });
  }

  submitMfa(): void {
    if (this.mfaLoading || !this.pendingMfaChallengeId) {
      return;
    }

    this.mfaError = null;
    if (!this.mfaCode.trim()) {
      this.mfaError = 'Lutfen mailinize gelen kodu girin.';
      return;
    }

    this.mfaLoading = true;
    this.authService.verifyMfa(this.pendingMfaChallengeId, this.mfaCode.trim()).subscribe({
      next: (response) => {
        this.mfaLoading = false;
        this.completeAuth({ ...response, mfaEnabled: true });
      },
      error: (err) => {
        this.mfaLoading = false;
        this.mfaError = err?.error?.message ?? 'Kod dogrulanamadi. Lutfen tekrar deneyin.';
      }
    });
  }

  backToLogin(): void {
    this.selectTab('login');
  }

  private activateMfaStep(challengeId: string, email: string): void {
    this.awaitingMfa = true;
    this.pendingMfaChallengeId = challengeId;
    this.pendingMfaEmail = email;
    this.mfaCode = '';
    this.mfaError = null;
  }

  private completeAuth(response: AuthResponse): void {
    this.authService.saveSession({ ...response, mfaEnabled: response.mfaEnabled ?? false });
    this.router.navigateByUrl(this.returnUrl || '/');
  }

  private resetMfaState(): void {
    this.awaitingMfa = false;
    this.pendingMfaChallengeId = null;
    this.pendingMfaEmail = '';
    this.mfaCode = '';
    this.mfaError = null;
  }

  private resetFeedback(): void {
    this.loginError = null;
    this.registerError = null;
    this.mfaError = null;
  }
}
