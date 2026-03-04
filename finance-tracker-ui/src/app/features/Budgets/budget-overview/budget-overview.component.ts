import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe, DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BudgetService } from '../../../core/services/budget.service';
import {
  BudgetSummaryDto,
  BudgetTrendDto,
  BudgetDto,
} from '../../../core/models/budget.model';

@Component({
  selector: 'app-budget-overview',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DecimalPipe, RouterLink, FormsModule],
  templateUrl: './budget-overview.component.html',
  styleUrls: ['./budget-overview.component.scss'],
})
export class BudgetOverviewComponent implements OnInit {
  summary = signal<BudgetSummaryDto | null>(null);
  trend = signal<BudgetTrendDto[]>([]);
  loading = signal(true);

  selectedMonth = signal(new Date().getMonth() + 1);
  selectedYear = signal(new Date().getFullYear());

  // Month navigation helpers
  readonly months = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];

  readonly years = Array.from(
    { length: 5 },
    (_, i) => new Date().getFullYear() - 2 + i,
  );

  // Max bar width for trend chart
  trendMax = computed(() => {
    const t = this.trend();
    if (!t.length) return 1;
    return Math.max(
      ...t.map((m) => Math.max(m.totalBudgeted, m.totalSpent)),
      1,
    );
  });

  constructor(private budgetService: BudgetService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.budgetService
      .getSummary(this.selectedMonth(), this.selectedYear())
      .subscribe({
        next: (s) => {
          this.summary.set(s);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
    this.budgetService.getTrend(6).subscribe({
      next: (t) => this.trend.set(t),
    });
  }

  prevMonth(): void {
    if (this.selectedMonth() === 1) {
      this.selectedMonth.set(12);
      this.selectedYear.set(this.selectedYear() - 1);
    } else {
      this.selectedMonth.set(this.selectedMonth() - 1);
    }
    this.load();
  }

  nextMonth(): void {
    const now = new Date();
    if (
      this.selectedYear() === now.getFullYear() &&
      this.selectedMonth() === now.getMonth() + 1
    )
      return;
    if (this.selectedMonth() === 12) {
      this.selectedMonth.set(1);
      this.selectedYear.set(this.selectedYear() + 1);
    } else {
      this.selectedMonth.set(this.selectedMonth() + 1);
    }
    this.load();
  }

  isCurrentMonth(): boolean {
    const now = new Date();
    return (
      this.selectedMonth() === now.getMonth() + 1 &&
      this.selectedYear() === now.getFullYear()
    );
  }

  barWidth(pct: number): string {
    return `${Math.min(pct, 100)}%`;
  }

  trendBarWidth(value: number): string {
    return `${(value / this.trendMax()) * 100}%`;
  }

  alertIcon(level: string): string {
    const icons: Record<string, string> = {
      warning:
        'M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z',
      critical:
        'M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z',
      exceeded:
        'M10 14l2-2m0 0l2-2m-2 2l-2-2m2 2l2 2m7-2a9 9 0 11-18 0 9 9 0 0118 0z',
    };
    return icons[level] ?? icons['warning'];
  }

  alertClass(level: string): string {
    return `alert-card alert-card--${level}`;
  }

  progressClass(level: string): string {
    return `progress-bar progress-bar--${level}`;
  }

  budgetStatusClass(b: BudgetDto): string {
    if (b.alertLevel === 'exceeded') return 'status status--exceeded';
    if (b.alertLevel === 'critical') return 'status status--critical';
    if (b.alertLevel === 'warning') return 'status status--warning';
    return 'status status--ok';
  }

  trackById(_: number, item: BudgetDto): string {
    return item.id;
  }
}
