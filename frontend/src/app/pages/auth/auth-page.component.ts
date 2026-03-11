import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService, LoginRequestDto, RegisterRequestDto } from '../../services/auth.service';

@Component({
  selector: 'app-auth-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './auth-page.component.html',
  styleUrl: './auth-page.component.scss'
})
export class AuthPageComponent {
  mode: 'login' | 'register' = 'login';

  email = '';
  password = '';
  role = 'User';
  merchantCategory = '';

  loading = signal(false);
  error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  switchMode(mode: 'login' | 'register') {
    this.mode = mode;
    this.error.set(null);
  }

  submit() {
    this.error.set(null);
    this.loading.set(true);

    if (this.mode === 'login') {
      const body: LoginRequestDto = {
        email: this.email,
        password: this.password
      };

      this.auth.login(body).subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.error?.message ?? 'Login failed');
        }
      });
    } else {
      const body: RegisterRequestDto = {
        email: this.email,
        password: this.password,
        role: this.role,
        merchantCategory: this.role === 'Merchant' ? this.merchantCategory : null
      };

      this.auth.register(body).subscribe({
        next: () => {
          this.loading.set(false);
          this.router.navigate(['/dashboard']);
        },
        error: (err) => {
          this.loading.set(false);
          this.error.set(err?.error?.error?.message ?? 'Registration failed');
        }
      });
    }
  }
}

