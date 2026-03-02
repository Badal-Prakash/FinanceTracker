import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../../core/services/invoice.service';
import {
  InvoiceListItem,
  InvoiceStats,
} from '../../../core/models/invoice.model';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, RouterLink, FormsModule],
  templateUrl: './invoice-list.component.html',
  styleUrls: ['./invoice-list.component.scss'],
})
export class InvoiceListComponent implements OnInit {
  protected readonly Math = Math;
  invoices = signal<InvoiceListItem[]>([]);
  stats = signal<InvoiceStats | null>(null);
  loading = signal(true);
  totalCount = signal(0);
  totalPages = signal(1);
  currentPage = signal(1);
  readonly pageSize = 20;
  filters = { status: '', clientName: '', fromDate: '', toDate: '' };

  constructor(private invoiceService: InvoiceService) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadList();
  }

  loadStats(): void {
    this.invoiceService
      .getStats()
      .subscribe({ next: (s) => this.stats.set(s) });
  }

  loadList(): void {
    this.loading.set(true);
    const active = Object.fromEntries(
      Object.entries(this.filters).filter(([, v]) => v !== ''),
    );
    this.invoiceService
      .getList({ page: this.currentPage(), pageSize: this.pageSize, ...active })
      .subscribe({
        next: (data) => {
          this.invoices.set(data.items);
          this.totalCount.set(data.totalCount);
          this.totalPages.set(data.totalPages);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onFilterChange(): void {
    this.currentPage.set(1);
    this.loadList();
  }

  clearFilters(): void {
    this.filters = { status: '', clientName: '', fromDate: '', toDate: '' };
    this.onFilterChange();
  }

  changePage(page: number): void {
    this.currentPage.set(page);
    this.loadList();
  }

  hasActiveFilters(): boolean {
    return !!(
      this.filters.status ||
      this.filters.clientName ||
      this.filters.fromDate ||
      this.filters.toDate
    );
  }

  isOverdue(inv: InvoiceListItem): boolean {
    return (
      inv.status === 'Overdue' ||
      (inv.status === 'Unpaid' && new Date(inv.dueDate) < new Date())
    );
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'badge badge--draft',
      Unpaid: 'badge badge--unpaid',
      Paid: 'badge badge--paid',
      Overdue: 'badge badge--overdue',
      Cancelled: 'badge badge--cancelled',
    };
    return map[status] ?? 'badge';
  }

  paginationStart(): number {
    return (this.currentPage() - 1) * this.pageSize + 1;
  }
  paginationEnd(): number {
    return Math.min(this.currentPage() * this.pageSize, this.totalCount());
  }
}
