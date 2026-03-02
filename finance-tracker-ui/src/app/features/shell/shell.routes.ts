import { Routes } from '@angular/router';
import { InvoiceListComponent } from '../invoices/invoice-list/invoice-list.component';
import { InvoiceFormComponent } from '../invoices/invoice-form/invoice-form.component';
import { InvoiceDetailComponent } from '../invoices/invoice-detail/invoice-detail.component';

export const SHELL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./shell.component').then((m) => m.ShellComponent),
    children: [
      {
        path: '',
        redirectTo: 'dashboard',
        pathMatch: 'full',
      },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('../dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },
      {
        path: 'expenses',
        loadComponent: () =>
          import('../expenses/expense-list/expense-list.component').then(
            (m) => m.ExpenseListComponent,
          ),
      },
      {
        path: 'expenses/new',
        loadComponent: () =>
          import('../expenses/expense-form/expense-form.component').then(
            (m) => m.ExpenseFormComponent,
          ),
      },
      {
        path: 'expenses/:id',
        loadComponent: () =>
          import('../expenses/expense-detail/expense-detail.component').then(
            (m) => m.ExpenseDetailComponent,
          ),
      },
      {
        path: 'invoices',
        children: [
          { path: '', component: InvoiceListComponent },
          { path: 'new', component: InvoiceFormComponent },
          { path: ':id', component: InvoiceDetailComponent },
          { path: ':id/edit', component: InvoiceFormComponent },
        ],
      },
    ],
  },
];
