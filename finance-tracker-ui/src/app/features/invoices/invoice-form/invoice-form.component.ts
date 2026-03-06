import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { InvoiceService } from '../../../core/services/invoice.service';
import { InvoiceDetail } from '../../../core/models/invoice.model';

interface LineItemForm {
  description: string;
  quantity: number;
  unitPrice: number;
}

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, RouterLink],
  templateUrl: './invoice-form.component.html',
  styleUrl: './invoice-form.component.scss',
})
export class InvoiceFormComponent implements OnInit {
  isEdit = signal(false);
  invoiceId = signal<string | null>(null);
  loading = signal(false);
  saving = signal(false);
  error = signal('');

  // Form fields
  clientName = '';
  clientEmail = '';
  clientAddress = '';
  dueDate = this.defaultDueDate();
  notes = '';
  lineItems = signal<LineItemForm[]>([
    { description: '', quantity: 1, unitPrice: 0 },
  ]);

  subtotal = computed(() =>
    this.lineItems().reduce((s, li) => s + li.quantity * li.unitPrice, 0),
  );

  constructor(
    private invoiceService: InvoiceService,
    private route: ActivatedRoute,
    private router: Router,
  ) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEdit.set(true);
      this.invoiceId.set(id);
      this.loadInvoice(id);
    }
  }

  loadInvoice(id: string): void {
    this.loading.set(true);
    this.invoiceService.getById(id).subscribe({
      next: (inv) => {
        this.clientName = inv.clientName;
        this.clientEmail = inv.clientEmail;
        this.clientAddress = inv.clientAddress ?? '';
        this.dueDate = inv.dueDate.split('T')[0];
        this.notes = inv.notes ?? '';
        this.lineItems.set(
          inv.lineItems.map((li) => ({
            description: li.description,
            quantity: li.quantity,
            unitPrice: li.unitPrice,
          })),
        );
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load invoice.');
        this.loading.set(false);
      },
    });
  }

  addLineItem(): void {
    this.lineItems.update((items) => [
      ...items,
      { description: '', quantity: 1, unitPrice: 0 },
    ]);
  }

  removeLineItem(i: number): void {
    if (this.lineItems().length === 1) return;
    this.lineItems.update((items) => items.filter((_, idx) => idx !== i));
  }

  updateLineItem(
    i: number,
    field: keyof LineItemForm,
    value: string | number,
  ): void {
    this.lineItems.update((items) =>
      items.map((li, idx) =>
        idx === i
          ? { ...li, [field]: field === 'description' ? value : Number(value) }
          : li,
      ),
    );
  }

  lineTotal(li: LineItemForm): number {
    return li.quantity * li.unitPrice;
  }

  submit(): void {
    this.error.set('');
    if (!this.clientName.trim()) {
      this.error.set('Client name is required.');
      return;
    }
    if (!this.clientEmail.trim()) {
      this.error.set('Client email is required.');
      return;
    }
    if (!this.dueDate) {
      this.error.set('Due date is required.');
      return;
    }

    const hasInvalidItem = this.lineItems().some(
      (li) => !li.description.trim() || li.quantity <= 0 || li.unitPrice <= 0,
    );
    if (hasInvalidItem) {
      this.error.set(
        'All line items must have a description, quantity > 0 and price > 0.',
      );
      return;
    }

    this.saving.set(true);
    const payload = {
      clientName: this.clientName.trim(),
      clientEmail: this.clientEmail.trim(),
      clientAddress: this.clientAddress.trim() || undefined,
      dueDate: new Date(this.dueDate).toISOString(),
      notes: this.notes.trim() || undefined,
      lineItems: this.lineItems().map((li) => ({
        description: li.description.trim(),
        quantity: li.quantity,
        unitPrice: li.unitPrice,
      })),
    };

    const req$ = (
      this.isEdit()
        ? this.invoiceService.update(this.invoiceId()!, payload)
        : this.invoiceService.create(payload)
    ) as Observable<string>;

    req$.subscribe({
      next: (id: any) => {
        this.saving.set(false);
        this.router.navigate([
          '/invoices',
          this.isEdit() ? this.invoiceId() : id,
        ]);
      },
      error: () => {
        this.error.set('Failed to save invoice. Please try again.');
        this.saving.set(false);
      },
    });
  }

  private defaultDueDate(): string {
    const d = new Date();
    d.setDate(d.getDate() + 30);
    return d.toISOString().split('T')[0];
  }
}
