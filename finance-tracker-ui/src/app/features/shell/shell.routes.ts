import { Routes } from '@angular/router';

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
          {
            path: '',
            loadComponent: () =>
              import('../invoices/invoice-list/invoice-list.component').then(
                (m) => m.InvoiceListComponent,
              ),
          },
          {
            path: 'new',
            loadComponent: () =>
              import('../invoices/invoice-form/invoice-form.component').then(
                (m) => m.InvoiceFormComponent,
              ),
          },
          {
            path: ':id',
            loadComponent: () =>
              import('../invoices/invoice-detail/invoice-detail.component').then(
                (m) => m.InvoiceDetailComponent,
              ),
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('../invoices/invoice-form/invoice-form.component').then(
                (m) => m.InvoiceFormComponent,
              ),
          },
        ],
      },
      {
        path: 'budgets',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('../Budgets/budget-overview/budget-overview.component').then(
                (m) => m.BudgetOverviewComponent,
              ),
          },
          {
            path: 'set',
            loadComponent: () =>
              import('../Budgets/budget-form/budget-form.component').then(
                (m) => m.BudgetFormComponent,
              ),
          },
        ],
      },
      {
        path: 'users',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('../users/user-list/user-list.component').then(
                (m) => m.UserListComponent,
              ),
          },
          {
            path: 'invite',
            loadComponent: () =>
              import('../users/invite-user/invite-user.component').then(
                (m) => m.InviteUserComponent,
              ),
          },
        ],
      },
      {
        path: 'profile',
        loadComponent: () =>
          import('../users/user-profile/user-profile.component').then(
            (m) => m.UserProfileComponent,
          ),
      },
      {
        path: 'reports',
        loadComponent: () =>
          import('../reports/reports.component').then(
            (m) => m.ReportsComponent,
          ),
      },
    ],
  },
];
