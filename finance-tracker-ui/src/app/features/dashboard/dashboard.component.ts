import {
  Component,
  OnInit,
  signal,
  computed,
  inject,
  ElementRef,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { environment } from '../../../environments/environment';

interface CategoryExpenseDto {
  categoryName: string;
  color: string;
  totalAmount: number;
  count: number;
  percentage: number;
}

interface MonthlyTrendDto {
  year: number;
  month: number;
  monthName: string;
  totalAmount: number;
  approvedAmount: number;
}

interface StatusBreakdownDto {
  status: string;
  count: number;
  totalAmount: number;
}

interface RecentExpenseDto {
  id: string;
  title: string;
  amount: number;
  status: string;
  categoryName: string;
  categoryColor: string;
  expenseDate: string;
  submittedBy: string;
}

interface DashboardStatsDto {
  totalExpensesThisMonth: number;
  totalExpensesLastMonth: number;
  monthOverMonthChange: number;
  pendingApprovalsCount: number;
  approvedThisMonth: number;
  rejectedThisMonth: number;
  approvalRate: number;
  totalApprovedAmountThisMonth: number;
  totalCategories: number;
  totalBudgetedThisMonth: number;
  budgetUtilisationPercent: number;
  topCategories: CategoryExpenseDto[];
  monthlyTrend: MonthlyTrendDto[];
  statusBreakdown: StatusBreakdownDto[];
  recentExpenses: RecentExpenseDto[];
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DecimalPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  user = this.authService.currentUser;
  loading = signal(true);
  error = signal(false);
  stats = signal<DashboardStatsDto | null>(null);
  greeting = signal('');

  // Chart helpers
  trendMax = computed(() => {
    const t = this.stats()?.monthlyTrend ?? [];
    return Math.max(...t.map((m) => m.totalAmount), 1);
  });

  donutSegments = computed(() => {
    const cats = this.stats()?.topCategories ?? [];
    let offset = 25; // start at top (25% into circle)
    const r = 15.9155; // radius for circumference=100
    return cats.map((c) => {
      const dash = c.percentage;
      const seg = { color: c.color, dash, offset, name: c.categoryName };
      offset = (offset - dash + 100) % 100;
      return seg;
    });
  });

  ngOnInit(): void {
    const h = new Date().getHours();
    this.greeting.set(
      h < 12 ? 'Good morning' : h < 18 ? 'Good afternoon' : 'Good evening',
    );
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.http.get<any>(`${environment.apiUrl}/dashboard/stats`).subscribe({
      next: (raw) => {
        // Normalise: fill missing fields from old backend with safe defaults
        const data: DashboardStatsDto = {
          totalExpensesThisMonth: raw.totalExpensesThisMonth ?? 0,
          totalExpensesLastMonth: raw.totalExpensesLastMonth ?? 0,
          monthOverMonthChange: raw.monthOverMonthChange ?? 0,
          pendingApprovalsCount: raw.pendingApprovalsCount ?? 0,
          approvedThisMonth: raw.approvedThisMonth ?? 0,
          rejectedThisMonth: raw.rejectedThisMonth ?? 0,
          approvalRate: raw.approvalRate ?? 0,
          totalApprovedAmountThisMonth: raw.totalApprovedAmountThisMonth ?? 0,
          totalCategories: raw.totalCategories ?? 0,
          totalBudgetedThisMonth: raw.totalBudgetedThisMonth ?? 0,
          budgetUtilisationPercent: raw.budgetUtilisationPercent ?? 0,
          topCategories: (raw.topCategories ?? []).map((c: any) => ({
            ...c,
            percentage: c.percentage ?? 0,
          })),
          monthlyTrend: (raw.monthlyTrend ?? []).map((m: any) => ({
            ...m,
            approvedAmount: m.approvedAmount ?? 0,
          })),
          statusBreakdown: raw.statusBreakdown ?? [],
          recentExpenses: raw.recentExpenses ?? [],
        };
        this.stats.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Dashboard API error', err);
        this.error.set(true);
        this.loading.set(false);
      },
    });
  }

  trendBarHeight(amount: number): string {
    const pct = (amount / this.trendMax()) * 100;
    return `${Math.max(pct, 2)}%`;
  }

  momClass(): string {
    const c = this.stats()?.monthOverMonthChange ?? 0;
    return c > 0 ? 'trend--up' : c < 0 ? 'trend--down' : 'trend--flat';
  }

  momIcon(): string {
    const c = this.stats()?.monthOverMonthChange ?? 0;
    return c > 0
      ? 'M5 10l7-7m0 0l7 7m-7-7v18'
      : c < 0
        ? 'M19 14l-7 7m0 0l-7-7m7 7V3'
        : 'M5 12h14';
  }

  budgetBarColor(): string {
    const p = this.stats()?.budgetUtilisationPercent ?? 0;
    if (p > 100) return '#dc2626';
    if (p >= 90) return '#f97316';
    if (p >= 80) return '#f59e0b';
    return '#4f46e5';
  }

  statusClass(s: string): string {
    const m: Record<string, string> = {
      Draft: 'status-draft',
      Submitted: 'status-submitted',
      Approved: 'status-approved',
      Rejected: 'status-rejected',
    };
    return m[s] ?? 'status-draft';
  }

  statusColor(s: string): string {
    const m: Record<string, string> = {
      Draft: '#94a3b8',
      Submitted: '#f59e0b',
      Approved: '#22c55e',
      Rejected: '#ef4444',
    };
    return m[s] ?? '#94a3b8';
  }

  totalStatusCount(): number {
    return (this.stats()?.statusBreakdown ?? []).reduce(
      (s, r) => s + r.count,
      0,
    );
  }
}
