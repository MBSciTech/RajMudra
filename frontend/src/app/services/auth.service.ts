import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../environments/environment';
import { tap } from 'rxjs/operators';

export interface AuthResultDto {
  userId: string;
  email: string;
  role: string;
  merchantCategory?: string | null;
  token: string;
}

export interface RegisterRequestDto {
  email: string;
  password: string;
  role: string;
  merchantCategory?: string | null;
}

export interface LoginRequestDto {
  email: string;
  password: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly tokenKey = 'rajmudra_token';
  private readonly roleKey = 'rajmudra_role';
  private readonly emailKey = 'rajmudra_email';
  private readonly userIdKey = 'rajmudra_userId';
  private readonly merchantCategoryKey = 'rajmudra_merchant_category';

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }

  get role(): string | null {
    return localStorage.getItem(this.roleKey);
  }

  get email(): string | null {
    return localStorage.getItem(this.emailKey);
  }

  get userId(): string | null {
    return localStorage.getItem(this.userIdKey);
  }

  get merchantCategory(): string | null {
    return localStorage.getItem(this.merchantCategoryKey);
  }

  login(body: LoginRequestDto) {
    return this.http
      .post<AuthResultDto>(`${environment.apiBaseUrl}/api/auth/login`, body)
      .pipe(tap((res) => this.setAuth(res)));
  }

  register(body: RegisterRequestDto) {
    return this.http
      .post<AuthResultDto>(`${environment.apiBaseUrl}/api/auth/register`, body)
      .pipe(tap((res) => this.setAuth(res)));
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.roleKey);
    localStorage.removeItem(this.emailKey);
    localStorage.removeItem(this.userIdKey);
    localStorage.removeItem(this.merchantCategoryKey);
    this.router.navigate(['/auth']);
  }

  private setAuth(res: AuthResultDto) {
    localStorage.setItem(this.tokenKey, res.token);
    localStorage.setItem(this.roleKey, res.role);
    localStorage.setItem(this.emailKey, res.email);
    localStorage.setItem(this.userIdKey, res.userId);
    if (res.merchantCategory) {
      localStorage.setItem(this.merchantCategoryKey, res.merchantCategory);
    } else {
      localStorage.removeItem(this.merchantCategoryKey);
    }
  }
}

