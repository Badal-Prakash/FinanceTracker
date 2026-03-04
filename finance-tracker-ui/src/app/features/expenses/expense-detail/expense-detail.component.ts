import { Component, OnInit, signal, Signal } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormControl,
  Validators,
} from '@angular/forms';
import { ExpenseService } from '../../../core/services/expense.service';
import { AuthService } from '../../../core/services/auth.service';
import { ExpenseDetail } from '../../../core/models/expense.model';
import { ReceiptUploadComponent } from '../../Receipt/receipt-upload/receipt-upload.component';
import { ReceiptService } from '../../../core/services/receipt.service';
import { ReceiptState } from '../../../features/Receipt/receipt-upload/receipt-upload.component';
@Component({
  selector: 'app-expense-detail',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
    ReceiptUploadComponent,
  ],
  templateUrl: './expense-detail.component.html',
  styleUrl: './expense-detail.component.scss',
})
export class ExpenseDetailComponent implements OnInit {
  expense = signal<ExpenseDetail | null>(null);
  loading = signal(true);
  actionLoading = signal(false);
  showRejectForm = signal(false);

  rejectReasonControl!: FormControl;
  canApprove!: Signal<boolean>; // ✅ explicit Signal<boolean> type

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseService: ExpenseService,
    private authService: AuthService,
    private fb: FormBuilder,
    private receiptService: ReceiptService,
  ) {
    this.rejectReasonControl = this.fb.control('', Validators.required);
    this.canApprove = this.authService.isManager; // ✅ isManager is Signal<boolean>
  }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.expenseService.getById(id).subscribe({
      next: (data) => {
        this.expense.set(data);
        this.loading.set(false);
      },
      error: () => this.router.navigate(['/expenses']),
    });
  }

  approve() {
    this.actionLoading.set(true);
    this.expenseService.approve(this.expense()!.id).subscribe({
      next: () => this.router.navigate(['/expenses']),
      error: () => this.actionLoading.set(false),
    });
  }

  reject() {
    if (!this.rejectReasonControl.value) return;
    this.actionLoading.set(true);
    this.expenseService
      .reject(this.expense()!.id, this.rejectReasonControl.value)
      .subscribe({
        next: () => this.router.navigate(['/expenses']),
        error: () => this.actionLoading.set(false),
      });
  }

  submit() {
    this.actionLoading.set(true);
    this.expenseService.submit(this.expense()!.id).subscribe({
      next: () => this.router.navigate(['/expenses']),
      error: () => this.actionLoading.set(false),
    });
  }

  statusBannerClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'banner-draft',
      Submitted: 'banner-submitted',
      Approved: 'banner-approved',
      Rejected: 'banner-rejected',
    };
    return map[status] ?? 'banner-draft';
  }

  statusIconClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'icon-draft',
      Submitted: 'icon-submitted',
      Approved: 'icon-approved',
      Rejected: 'icon-rejected',
    };
    return map[status] ?? 'icon-draft';
  }

  onReceiptUploaded(receiptState: ReceiptState): void {
    // Update the expense with the new receipt URL
    if (this.expense() && receiptState.url) {
      this.expense.update((current) => ({
        ...current!,
        receiptUrl: receiptState.url ?? undefined,
      }));
    }
  }

  onReceiptRemoved(): void {
    // Clear the receipt URL from the expense
    if (this.expense()) {
      this.expense.update((current) => ({
        ...current!,
        receiptUrl: undefined,
      }));
    }
  }
}
