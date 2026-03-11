import { Routes } from '@angular/router';
import { AuthPageComponent } from './pages/auth/auth-page.component';
import { DashboardPageComponent } from './pages/dashboard/dashboard-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'auth' },
  { path: 'auth', component: AuthPageComponent },
  { path: 'dashboard', component: DashboardPageComponent }
];
