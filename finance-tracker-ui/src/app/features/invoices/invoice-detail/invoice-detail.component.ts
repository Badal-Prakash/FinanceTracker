import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { InvoiceService } from '../../../core/services/invoice.service';
import { AuthService } from '../../../core/services/auth.service';
import { InvoiceDetail } from '../../../core/models/invoice.model';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, RouterLink],
  templateUrl: './invoice-detail.component.html',
  styleUrls: ['./invoice-detail.component.scss'],
})
export class InvoiceDetailComponent implements OnInit {
  invoice = signal<InvoiceDetail | null>(null);
  loading = signal(true);
  actionLoading = signal(false);
  canAct = signal(false);
  canDelete = signal(false);

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private invoiceService: InvoiceService,
    private authService: AuthService,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    const role = this.authService.currentUser()?.role ?? '';
    this.canAct.set(['Admin', 'SuperAdmin', 'Manager'].includes(role));
    this.canDelete.set(['Admin', 'SuperAdmin'].includes(role));
    this.invoiceService.getById(id).subscribe({
      next: (data) => {
        this.invoice.set(data);
        this.loading.set(false);
      },
      error: () => this.router.navigate(['/invoices']),
    });
  }

  sendInvoice(): void {
    this.actionLoading.set(true);
    this.invoiceService.send(this.invoice()!.id).subscribe({
      next: () => this.reload(),
      error: () => this.actionLoading.set(false),
    });
  }

  markPaid(): void {
    this.actionLoading.set(true);
    this.invoiceService.markPaid(this.invoice()!.id).subscribe({
      next: () => this.reload(),
      error: () => this.actionLoading.set(false),
    });
  }

  cancelInvoice(): void {
    if (!confirm('Cancel this invoice?')) return;
    this.actionLoading.set(true);
    this.invoiceService.cancel(this.invoice()!.id).subscribe({
      next: () => this.reload(),
      error: () => this.actionLoading.set(false),
    });
  }

  deleteInvoice(): void {
    if (!confirm('Delete this invoice? This cannot be undone.')) return;
    this.actionLoading.set(true);
    this.invoiceService.delete(this.invoice()!.id).subscribe({
      next: () => this.router.navigate(['/invoices']),
      error: () => this.actionLoading.set(false),
    });
  }

  private reload(): void {
    const id = this.invoice()!.id;
    this.actionLoading.set(false);
    this.invoiceService.getById(id).subscribe((data) => this.invoice.set(data));
  }

  isOverdue(inv: InvoiceDetail): boolean {
    return (
      inv.status === 'Overdue' ||
      (inv.status === 'Unpaid' && new Date(inv.dueDate) < new Date())
    );
  }

  daysOverdue(inv: InvoiceDetail): number {
    return Math.floor(
      (Date.now() - new Date(inv.dueDate).getTime()) / 86400000,
    );
  }

  lineTotal(quantity: number, unitPrice: number): number {
    return quantity * unitPrice;
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

  bannerClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'banner banner--neutral',
      Unpaid: 'banner banner--warning',
      Paid: 'banner banner--success',
      Overdue: 'banner banner--danger',
      Cancelled: 'banner banner--neutral',
    };
    return map[status] ?? 'banner';
  }
}
