import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { Router,RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { ExpenseService } from '../../core/services/expense.service';
import { environment } from '../../../environments/environment';

interface DashboardStats {
  totalExpensesThisMonth: number;
  pendingApprovalsCount: number;
  approvedThisMonth: number;
  totalCategories: number;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private expenseService = inject(ExpenseService);
  private authService = inject(AuthService);

  user = this.authService.currentUser;
  loading = signal(true);
  stats = signal<DashboardStats | null>(null);
  recentExpenses = signal<any[]>([]);

  greeting = signal('');

  ngOnInit() {
    const hour = new Date().getHours();
    if (hour < 12) this.greeting.set('morning');
    else if (hour < 18) this.greeting.set('afternoon');
    else this.greeting.set('evening');

    this.loadDashboard();
  }

  loadDashboard() {
    this.loading.set(true);

    this.expenseService.getList({ page: 1, pageSize: 5 }).subscribe({
      next: (data) => {
        this.recentExpenses.set(data.items);

        const thisMonth = data.items.filter((e) => {
          const d = new Date(e.expenseDate);
          const now = new Date();
          return (
            d.getMonth() === now.getMonth() &&
            d.getFullYear() === now.getFullYear()
          );
        });

        this.stats.set({
          totalExpensesThisMonth: thisMonth.reduce(
            (sum, e) => sum + e.amount,
            0,
          ),
          pendingApprovalsCount: data.items.filter(
            (e) => e.status === 'Submitted',
          ).length,
          approvedThisMonth: thisMonth.filter((e) => e.status === 'Approved')
            .length,
          totalCategories: new Set(data.items.map((e) => e.categoryName)).size,
        });

        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'bg-gray-100 text-gray-700',
      Submitted: 'bg-yellow-100 text-yellow-700',
      Approved: 'bg-green-100 text-green-700',
      Rejected: 'bg-red-100 text-red-700',
    };
    return map[status] ?? 'bg-gray-100 text-gray-700';
  }
}
