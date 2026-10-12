import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { UserListItem } from '../../models/user';
import { UserService } from '../../services/user.service';
import { extractArray } from '../../utils/response-array';

@Component({
  selector: 'app-admin-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {
  users: UserListItem[] = [];
  loading = false;
  error: string | null = null;
  saving: Record<number, boolean> = {};
  deleting: Record<number, boolean> = {};
  pendingRoles: Record<number, string> = {};

  constructor(private userService: UserService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.error = null;

    this.userService
      .getUsers()
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (users) => {
          this.applyUsers(extractArray<UserListItem>(users));
        },
        error: (err) => {
          this.error =
            err?.status === 401 || err?.status === 403
              ? 'Bu sayfa icin admin yetkisi gerekiyor.'
              : 'Uyeler yuklenirken hata olustu.';
        }
      });
  }

  updateRole(user: UserListItem): void {
    const desiredRole = this.pendingRoles[user.id];
    if (!desiredRole) {
      return;
    }

    this.saving[user.id] = true;
    this.userService.updateRole(user.id, desiredRole).subscribe({
      next: () => {
        user.role = desiredRole;
        this.saving[user.id] = false;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Rol guncellenemedi.';
        this.saving[user.id] = false;
      }
    });
  }

  deleteUser(user: UserListItem): void {
    if (this.deleting[user.id]) {
      return;
    }

    if (!confirm(`Kullaniciyi silmek istiyor musunuz? (${user.email})`)) {
      return;
    }

    this.deleting[user.id] = true;
    this.userService.deleteUser(user.id).subscribe({
      next: () => {
        this.users = this.users.filter((item) => item.id !== user.id);
        delete this.pendingRoles[user.id];
        this.deleting[user.id] = false;
      },
      error: (err) => {
        this.error = err?.error?.message ?? 'Kullanici silinemedi.';
        this.deleting[user.id] = false;
      }
    });
  }

  private applyUsers(users: UserListItem[]): void {
    this.users = users;
    this.pendingRoles = {};
    users.forEach((user) => {
      this.pendingRoles[user.id] = user.role;
    });
  }
}
