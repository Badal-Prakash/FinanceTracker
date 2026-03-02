import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { InvoiceListComponent } from './features/invoices/invoice-list/invoice-list.component';
import { InvoiceDetailComponent } from './features/invoices/invoice-detail/invoice-detail.component';
import { InvoiceFormComponent } from './features/invoices/invoice-form/invoice-form.component';

const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((m) => m.AUTH_ROUTES),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/shell/shell.routes').then((m) => m.SHELL_ROUTES),
  },
  // {
  //   path: 'invoices',
  //   children: [
  //     { path: '', component: InvoiceListComponent },
  //     { path: 'new', component: InvoiceFormComponent },
  //     { path: ':id', component: InvoiceDetailComponent },
  //     { path: ':id/edit', component: InvoiceFormComponent },
  //   ],
  // },
  { path: '**', redirectTo: '' },
];
export default routes;
