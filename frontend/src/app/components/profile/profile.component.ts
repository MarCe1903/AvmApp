import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService, AuthResponse } from '../../services/auth.service';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  session: AuthResponse | null = null;
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  passwordError: string | null = null;
  passwordSuccess: string | null = null;
  savingPassword = false;
  mfaEnabled = false;
  savingMfa = false;
  mfaError: string | null = null;
  mfaSuccess: string | null = null;

  constructor(private auth: AuthService) {}

  ngOnInit(): void {
    this.session = this.auth.getSession();
    this.mfaEnabled = this.session?.mfaEnabled ?? false;
  }

  updatePassword(): void {
    if (this.savingPassword) return;
    this.passwordError = null;
    this.passwordSuccess = null;

    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.passwordError = 'Tum alanlari doldurun.';
      return;
    }

    if (this.newPassword !== this.confirmPassword) {
      this.passwordError = 'Yeni sifreler eslesmiyor.';
      return;
    }

    this.savingPassword = true;
    this.auth.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.passwordSuccess = 'Sifre guncellendi.';
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.savingPassword = false;
      },
      error: (err) => {
        this.passwordError = err?.error?.message ?? 'Sifre guncellenemedi.';
        this.savingPassword = false;
      }
    });
  }

  toggleMfa(): void {
    if (this.savingMfa) return;

    const nextEnabled = !this.mfaEnabled;
    this.savingMfa = true;
    this.mfaError = null;
    this.mfaSuccess = null;

    this.auth.toggleMfa(nextEnabled).subscribe({
      next: () => {
        this.mfaEnabled = nextEnabled;
        this.auth.updateSession({ mfaEnabled: nextEnabled });
        this.session = this.auth.getSession();
        this.mfaSuccess = nextEnabled
          ? 'Mail ile iki adimli dogrulama aktif edildi.'
          : 'Mail ile iki adimli dogrulama kapatildi.';
        this.savingMfa = false;
      },
      error: (err) => {
        this.mfaError = err?.error?.message ?? 'MFA ayari guncellenemedi.';
        this.savingMfa = false;
      }
    });
  }
}
