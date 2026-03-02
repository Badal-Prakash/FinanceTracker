import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormArray,
  Validators,
  FormGroup,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Observable } from 'rxjs';
import { InvoiceService } from '../../../core/services/invoice.service';

@Component({
  selector: 'app-invoice-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, CurrencyPipe],
  templateUrl: './invoice-form.component.html',
  styleUrls: ['./invoice-form.component.scss'],
})
export class InvoiceFormComponent implements OnInit {
  form!: FormGroup;

  loading = signal(false);
  errorMessage = signal('');
  isEdit = false;
  editId: string | null = null;

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private router: Router,
    private route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.initializeForm();
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.editId;
    if (this.isEdit) {
      this.invoiceService.getById(this.editId!).subscribe({
        next: (inv) => {
          this.form.patchValue({
            clientName: inv.clientName,
            clientEmail: inv.clientEmail,
            clientAddress: inv.clientAddress ?? '',
            dueDate: inv.dueDate.split('T')[0],
            notes: inv.notes ?? '',
          });
          while (this.lineItems.length) this.lineItems.removeAt(0);
          inv.lineItems.forEach((li) => {
            this.lineItems.push(
              this.fb.group({
                description: [li.description, Validators.required],
                quantity: [
                  li.quantity,
                  [Validators.required, Validators.min(1)],
                ],
                unitPrice: [
                  li.unitPrice,
                  [Validators.required, Validators.min(0)],
                ],
              }),
            );
          });
        },
        error: () => this.router.navigate(['/invoices']),
      });
    } else {
      const due = new Date();
      due.setDate(due.getDate() + 30);
      this.form.patchValue({ dueDate: due.toISOString().split('T')[0] });
    }
  }

  get lineItems(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  initializeForm(): void {
    this.form = this.fb.group({
      clientName: ['', [Validators.required, Validators.maxLength(200)]],
      clientEmail: ['', [Validators.required, Validators.email]],
      clientAddress: [''],
      dueDate: ['', Validators.required],
      notes: [''],
      lineItems: this.fb.array([this.createLineItemGroup()]),
    });
  }

  createLineItemGroup(): FormGroup {
    return this.fb.group({
      description: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
    });
  }

  addLineItem(): void {
    this.lineItems.push(this.createLineItemGroup());
  }

  removeLineItem(index: number): void {
    this.lineItems.removeAt(index);
  }

  lineItemTotal(index: number): number {
    const v = this.lineItems.at(index).value;
    return (Number(v.quantity) || 0) * (Number(v.unitPrice) || 0);
  }

  invoiceTotal(): number {
    return this.lineItems.controls.reduce((s, c) => {
      const v = c.value;
      return s + (Number(v.quantity) || 0) * (Number(v.unitPrice) || 0);
    }, 0);
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.loading.set(true);
    this.errorMessage.set('');
    const payload = {
      clientName: this.form.value.clientName!,
      clientEmail: this.form.value.clientEmail!,
      clientAddress: this.form.value.clientAddress || undefined,
      dueDate: this.form.value.dueDate!,
      notes: this.form.value.notes || undefined,
      lineItems: this.lineItems.value.map((li: any) => ({
        description: li.description,
        quantity: Number(li.quantity),
        unitPrice: Number(li.unitPrice),
      })),
    };
    const req: Observable<any> = this.isEdit
      ? this.invoiceService.update(this.editId!, payload)
      : this.invoiceService.create(payload);
    req.subscribe({
      next: () => this.router.navigate(['/invoices']),
      error: (err: any) => {
        this.loading.set(false);
        this.errorMessage.set(err.error?.title || 'Failed to save invoice.');
      },
    });
  }
}
