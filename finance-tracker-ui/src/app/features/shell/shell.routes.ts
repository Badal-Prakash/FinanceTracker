import { Routes } from '@angular/router';
import { roleGuard } from '../../core/guards/auth.guard';

export const SHELL_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./shell.component').then((m) => m.ShellComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },

      // ── Dashboard ──────────────────────────────────────────────────────────
      {
        path: 'dashboard',
        loadComponent: () =>
          import('../dashboard/dashboard.component').then(
            (m) => m.DashboardComponent,
          ),
      },

      // ── Expenses ───────────────────────────────────────────────────────────
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

      // ── Invoices ───────────────────────────────────────────────────────────
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

      // ── Budgets — Manager+ only ────────────────────────────────────────────
      {
        path: 'budgets',
        canActivate: [roleGuard('Manager', 'Admin', 'SuperAdmin')],
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

      // ── Users — Admin+ only ────────────────────────────────────────────────
      {
        path: 'users',
        canActivate: [roleGuard('Admin', 'SuperAdmin')],
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

      // ── Team — Admin+ only ─────────────────────────────────────────────────
      {
        path: 'team',
        canActivate: [roleGuard('Admin', 'SuperAdmin')],
        loadComponent: () =>
          import('../team/team-list/team-list.component').then(
            (m) => m.TeamListComponent,
          ),
      },

      // ── Profile ────────────────────────────────────────────────────────────
      {
        path: 'profile',
        loadComponent: () =>
          import('../users/user-profile/user-profile.component').then(
            (m) => m.UserProfileComponent,
          ),
      },

      // ── Reports ────────────────────────────────────────────────────────────
      {
        path: 'reports',
        loadComponent: () =>
          import('../reports/reports.component').then(
            (m) => m.ReportsComponent,
          ),
      },

      {
        path: 'notifications',
        loadComponent: () =>
          import('../notification/notifications-page/notifications-page.component').then(
            (m) => m.NotificationsPageComponent,
          ),
      },
      {
        path: 'audit-log',
        canActivate: [roleGuard('Admin', 'SuperAdmin')],
        loadComponent: () =>
          import('../audit-log/audit-log.component').then(
            (m) => m.AuditLogComponent,
          ),
      },
      {
        path: 'forbidden',
        loadComponent: () =>
          import('../errors/forbidden/forbidden.component').then(
            (m) => m.ForbiddenComponent,
          ),
      },
    ],
  },
];
