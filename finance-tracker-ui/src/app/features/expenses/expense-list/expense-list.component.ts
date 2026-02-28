import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  ExpenseService,
  ExpenseListItem,
  PaginatedList,
} from '../../../core/services/expense.service';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss',
})
export class ExpenseListComponent implements OnInit {
  data = signal<PaginatedList<ExpenseListItem> | null>(null);
  loading = signal(true);
  currentStatus = signal<string>('');
  currentPage = signal(1);

  statuses = ['', 'Draft', 'Submitted', 'Approved', 'Rejected'];

  constructor(private expenseService: ExpenseService) {}

  ngOnInit() {
    this.loadExpenses();
  }

  loadExpenses() {
    this.loading.set(true);
    this.expenseService
      .getList({
        page: this.currentPage(),
        pageSize: 20,
        status: this.currentStatus() || undefined,
      })
      .subscribe({
        next: (data) => {
          this.data.set(data);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  filterByStatus(status: string) {
    this.currentStatus.set(status);
    this.currentPage.set(1);
    this.loadExpenses();
  }

  changePage(page: number) {
    this.currentPage.set(page);
    this.loadExpenses();
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
