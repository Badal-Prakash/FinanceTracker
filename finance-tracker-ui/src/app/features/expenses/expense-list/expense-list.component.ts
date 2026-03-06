import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ExpenseService } from '../../../core/services/expense.service';
import {
  ExpenseListItem,
  PaginatedList,
} from '../../../core/models/expense.model';
import { ExpenseImportComponent } from '../expense-import/expense-import.component';

@Component({
  selector: 'app-expense-list',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    ExpenseImportComponent,
  ],
  templateUrl: './expense-list.component.html',
  styleUrl: './expense-list.component.scss',
})
export class ExpenseListComponent implements OnInit {
  data = signal<PaginatedList<ExpenseListItem> | null>(null);
  loading = signal(true);
  currentStatus = signal<string>('');
  currentPage = signal(1);

  statuses = ['', 'Draft', 'Submitted', 'Approved', 'Rejected'];
  showImport = signal(false);

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

  onImportClosed(success: boolean): void {
    this.showImport.set(false);
    if (success) this.loadExpenses();
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'status-draft',
      Submitted: 'status-submitted',
      Approved: 'status-approved',
      Rejected: 'status-rejected',
    };
    return map[status] ?? 'status-draft';
  }
}
