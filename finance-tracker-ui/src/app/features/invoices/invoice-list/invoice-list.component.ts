import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoiceService } from '../../../core/services/invoice.service';
import {
  InvoiceListItem,
  InvoiceStats,
  InvoiceFilters,
} from '../../../core/models/invoice.model';

@Component({
  selector: 'app-invoice-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, RouterLink, FormsModule],
  templateUrl: './invoice-list.component.html',
  styleUrl: './invoice-list.component.scss',
})
export class InvoiceListComponent implements OnInit {
  invoices = signal<InvoiceListItem[]>([]);
  stats = signal<InvoiceStats | null>(null);
  loading = signal(true);
  totalCount = signal(0);
  page = signal(1);
  totalPages = signal(1);

  filters: InvoiceFilters = { page: 1, pageSize: 20 };
  search = '';
  statusFilter = '';

  readonly statuses = ['Unpaid', 'Paid', 'Overdue', 'Cancelled'];

  constructor(
    private invoiceService: InvoiceService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadList();
  }

  loadStats(): void {
    this.invoiceService.getStats().subscribe({
      next: (s) => this.stats.set(s),
    });
  }

  loadList(): void {
    this.loading.set(true);
    const f: InvoiceFilters = {
      ...this.filters,
      page: this.page(),
      clientName: this.search || undefined,
      status: this.statusFilter || undefined,
    };
    this.invoiceService.getList(f).subscribe({
      next: (data) => {
        this.invoices.set(data.items);
        this.totalCount.set(data.totalCount);
        this.totalPages.set(data.totalPages);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onSearch(): void {
    this.page.set(1);
    this.loadList();
  }
  onFilter(): void {
    this.page.set(1);
    this.loadList();
  }
  prevPage(): void {
    if (this.page() > 1) {
      this.page.update((p) => p - 1);
      this.loadList();
    }
  }
  nextPage(): void {
    if (this.page() < this.totalPages()) {
      this.page.update((p) => p + 1);
      this.loadList();
    }
  }

  markPaid(id: string, e: Event): void {
    e.stopPropagation();
    this.invoiceService.markPaid(id).subscribe({ next: () => this.loadList() });
  }

  deleteInvoice(id: string, e: Event): void {
    e.stopPropagation();
    if (!confirm('Delete this invoice?')) return;
    this.invoiceService.delete(id).subscribe({ next: () => this.loadList() });
  }

  statusClass(status: string): string {
    const m: Record<string, string> = {
      Unpaid: 'badge--unpaid',
      Paid: 'badge--paid',
      Overdue: 'badge--overdue',
      Cancelled: 'badge--cancelled',
    };
    return 'badge ' + (m[status] ?? '');
  }

  isOverdue(invoice: InvoiceListItem): boolean {
    return (
      invoice.status === 'Unpaid' && new Date(invoice.dueDate) < new Date()
    );
  }
}
