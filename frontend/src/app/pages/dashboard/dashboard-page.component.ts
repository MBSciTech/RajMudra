import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { FormsModule } from '@angular/forms';

interface TokenDto {
  id: string;
  ownerId: string;
  denomination: number;
  createdAt: string;
  isSpent: boolean;
  purpose?: string | null;
}

interface AdminUserDto {
  id: string;
  email: string;
  role: string;
  merchantCategory?: string | null;
}

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss'
})
export class DashboardPageComponent implements OnInit {
  balance: number | null = null;
  tokens: TokenDto[] = [];

  // Admin mint form
  mintUserId = '';
  mintAmount: number | null = null;
  mintPurpose = '';

  // Transfer form
  transferTokenId = '';
  transferRecipientId = '';
  transferAmount: number | null = null;

  // Merchant redeem form
  redeemTokenId = '';

  // Admin: users management
  users: AdminUserDto[] = [];
  selectedUser: AdminUserDto | null = null;
  editEmail = '';
  editRole = 'User';
  editMerchantCategory = '';
  resetPassword = '';

  loading = false;
  message = '';
  error = '';

  private readonly http = inject(HttpClient);

  constructor(public readonly auth: AuthService, private readonly router: Router) {}

  ngOnInit(): void {
    if (!this.auth.isAuthenticated) {
      this.router.navigate(['/auth']);
      return;
    }
    this.refreshWallet();
    if (this.isAdmin) {
      this.loadUsers();
    }
  }

  get isAdmin(): boolean {
    return this.auth.role === 'Admin';
  }

  get isMerchant(): boolean {
    return this.auth.role === 'Merchant';
  }

  get isUser(): boolean {
    return !this.isAdmin && !this.isMerchant;
  }

  logout() {
    this.auth.logout();
  }

  refreshWallet() {
    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .get<{ balance: number }>(`${environment.apiBaseUrl}/api/wallet/balance`)
      .subscribe({
        next: (res: { balance: number }) => {
          this.balance = res.balance;
        },
        error: (err: unknown) => {
          this.error = 'Failed to load balance.';
          console.error(err);
          this.loading = false;
        }
      });

    this.http.get<TokenDto[]>(`${environment.apiBaseUrl}/api/wallet/tokens`).subscribe({
      next: (tokens: TokenDto[]) => {
        this.tokens = tokens;
        this.loading = false;
      },
      error: (err: unknown) => {
        this.error = 'Failed to load tokens.';
        console.error(err);
        this.loading = false;
      }
    });
  }

  loadUsers() {
    this.http.get<AdminUserDto[]>(`${environment.apiBaseUrl}/api/admin/users`).subscribe({
      next: (users: AdminUserDto[]) => {
        this.users = users;
      },
      error: (err: unknown) => {
        console.error(err);
        this.error = 'Failed to load users.';
      }
    });
  }

  selectUser(u: AdminUserDto) {
    this.selectedUser = u;
    this.editEmail = u.email;
    this.editRole = u.role;
    this.editMerchantCategory = u.merchantCategory ?? '';
    this.resetPassword = '';
  }

  clearSelectedUser() {
    this.selectedUser = null;
    this.editEmail = '';
    this.editRole = 'User';
    this.editMerchantCategory = '';
    this.resetPassword = '';
  }

  saveUser() {
    if (!this.selectedUser) return;
    if (!this.editEmail.trim() || !this.editRole.trim()) {
      this.error = 'Email and role are required.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .put<AdminUserDto>(`${environment.apiBaseUrl}/api/admin/users/${this.selectedUser.id}`, {
        email: this.editEmail.trim(),
        role: this.editRole.trim(),
        merchantCategory: this.editMerchantCategory.trim() ? this.editMerchantCategory.trim() : null
      })
      .subscribe({
        next: (updated: AdminUserDto) => {
          this.message = 'User updated.';
          this.loading = false;
          this.selectedUser = updated;
          this.loadUsers();
        },
        error: (err: unknown) => {
          console.error(err);
          this.error = 'Failed to update user.';
          this.loading = false;
        }
      });
  }

  deleteUser(u: AdminUserDto) {
    if (!confirm(`Delete user ${u.email}?`)) return;

    this.loading = true;
    this.error = '';
    this.message = '';

    this.http.delete(`${environment.apiBaseUrl}/api/admin/users/${u.id}`).subscribe({
      next: () => {
        this.message = 'User deleted.';
        this.loading = false;
        if (this.selectedUser?.id === u.id) {
          this.clearSelectedUser();
        }
        this.loadUsers();
      },
      error: (err: unknown) => {
        console.error(err);
        this.error = 'Failed to delete user.';
        this.loading = false;
      }
    });
  }

  submitResetPassword() {
    if (!this.selectedUser) return;
    if (!this.resetPassword.trim() || this.resetPassword.trim().length < 6) {
      this.error = 'New password must be at least 6 characters.';
      return;
    }

    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .post(`${environment.apiBaseUrl}/api/admin/users/${this.selectedUser.id}/reset-password`, {
        newPassword: this.resetPassword.trim()
      })
      .subscribe({
        next: () => {
          this.message = 'Password reset successfully.';
          this.resetPassword = '';
          this.loading = false;
        },
        error: (err: unknown) => {
          console.error(err);
          this.error = 'Failed to reset password.';
          this.loading = false;
        }
      });
  }

  submitMint() {
    if (!this.mintUserId || !this.mintAmount || this.mintAmount <= 0) {
      this.error = 'Enter user id and a positive amount.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .post<TokenDto>(`${environment.apiBaseUrl}/api/admin/mint`, {
        userId: this.mintUserId,
        denomination: this.mintAmount,
        purpose: this.mintPurpose || null
      })
      .subscribe({
        next: () => {
          this.message = 'Voucher minted successfully.';
          this.mintAmount = null;
          this.mintPurpose = '';
          this.refreshWallet();
        },
        error: (err: unknown) => {
          this.error = 'Failed to mint voucher.';
          console.error(err);
          this.loading = false;
        }
      });
  }

  submitTransfer() {
    if (!this.transferTokenId || !this.transferRecipientId || !this.transferAmount || this.transferAmount <= 0) {
      this.error = 'Fill all transfer fields with valid values.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .post(`${environment.apiBaseUrl}/api/tokens/transfer`, {
        tokenId: this.transferTokenId,
        recipientId: this.transferRecipientId,
        amount: this.transferAmount
      })
      .subscribe({
        next: () => {
          this.message = 'Transfer completed.';
          this.transferAmount = null;
          this.refreshWallet();
        },
        error: (err: unknown) => {
          this.error = 'Failed to transfer.';
          console.error(err);
          this.loading = false;
        }
      });
  }

  submitRedeem() {
    if (!this.redeemTokenId) {
      this.error = 'Enter a token id to redeem.';
      return;
    }
    this.loading = true;
    this.error = '';
    this.message = '';

    this.http
      .post(`${environment.apiBaseUrl}/api/tokens/redeem`, {
        tokenId: this.redeemTokenId
      })
      .subscribe({
        next: () => {
          this.message = 'Redemption successful.';
          this.refreshWallet();
        },
        error: (err: unknown) => {
          this.error = 'Failed to redeem.';
          console.error(err);
          this.loading = false;
        }
      });
  }
}

