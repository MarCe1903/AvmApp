import { Routes } from '@angular/router';
import { ProductListComponent } from './components/product-list/product-list';
import { AdminPanelComponent } from './components/admin-panel/admin-panel.component';
import { ProfileComponent } from './components/profile/profile.component';
import { OrdersComponent } from './components/orders/orders.component';
import { SellerPanelComponent } from './components/seller-panel/seller-panel.component';
import { AuthPageComponent } from './components/auth-page/auth-page.component';
import { adminGuard } from './guards/admin.guard';
import { authGuard } from './guards/auth.guard';
import { sellerGuard } from './guards/seller.guard';

export const routes: Routes = [
  { path: '', component: ProductListComponent },
  { path: 'auth', component: AuthPageComponent },
  { path: 'admin', component: AdminPanelComponent, canActivate: [adminGuard] },
  { path: 'seller', component: SellerPanelComponent, canActivate: [sellerGuard] },
  { path: 'profile', component: ProfileComponent, canActivate: [authGuard] },
  { path: 'orders', component: OrdersComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: '' }
];
