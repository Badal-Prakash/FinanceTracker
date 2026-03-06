import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { InvoiceService } from '../../../core/services/invoice.service';
import { InvoiceDetail } from '../../../core/models/invoice.model';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './invoice-detail.component.html',
  styleUrl: './invoice-detail.component.scss',
})
export class InvoiceDetailComponent implements OnInit {
  invoice = signal<InvoiceDetail | null>(null);
  loading = signal(true);
  acting = signal(false);
  error = signal('');

  constructor(
    private invoiceService: InvoiceService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.invoiceService.getById(id).subscribe({
      next: (inv) => {
        this.invoice.set(inv);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load invoice.');
        this.loading.set(false);
      },
    });
  }

  markPaid(): void {
    if (!confirm('Mark this invoice as paid?')) return;
    this.acting.set(true);
    this.invoiceService.markPaid(this.invoice()!.id).subscribe({
      next: () => {
        this.invoice.update((inv) => (inv ? { ...inv, status: 'Paid' } : inv));
        this.acting.set(false);
      },
      error: () => this.acting.set(false),
    });
  }

  cancel(): void {
    if (!confirm('Cancel this invoice? This cannot be undone.')) return;
    this.acting.set(true);
    this.invoiceService.cancel(this.invoice()!.id).subscribe({
      next: () => {
        this.invoice.update((inv) =>
          inv ? { ...inv, status: 'Cancelled' } : inv,
        );
        this.acting.set(false);
      },
      error: () => this.acting.set(false),
    });
  }

  delete(): void {
    if (!confirm('Delete this invoice permanently?')) return;
    this.acting.set(true);
    this.invoiceService.delete(this.invoice()!.id).subscribe({
      next: () => this.router.navigate(['/invoices']),
      error: () => this.acting.set(false),
    });
  }

  printInvoice(): void {
    window.print();
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

  isEditable(): boolean {
    const s = this.invoice()?.status;
    return s === 'Unpaid' || s === 'Overdue';
  }
}
