import { Component, OnInit, signal } from '@angular/core';
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

@Component({
  selector: 'app-expense-detail',
  standalone: true,
  imports: [
    CommonModule,
    CurrencyPipe,
    DatePipe,
    RouterLink,
    ReactiveFormsModule,
  ],
  templateUrl: './expense-detail.component.html',
  styleUrl: './expense-detail.component.scss',
})
export class ExpenseDetailComponent implements OnInit {
  expense = signal<ExpenseDetail | null>(null);
  loading = signal(true);
  actionLoading = signal(false);
  showRejectForm = signal(false);

  // ✅ declare types only — initialize in constructor
  rejectReasonControl!: FormControl;
  canApprove!: ReturnType<AuthService['isManager']['call']>;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private expenseService: ExpenseService,
    private authService: AuthService,
    private fb: FormBuilder,
  ) {
    // ✅ both initialized here so all dependencies are ready
    this.rejectReasonControl = this.fb.control('', Validators.required);
    this.canApprove = this.authService.isManager;
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
      Draft: 'bg-gray-50 text-gray-700',
      Submitted: 'bg-yellow-50 text-yellow-700',
      Approved: 'bg-green-50 text-green-700',
      Rejected: 'bg-red-50 text-red-700',
    };
    return map[status] ?? 'bg-gray-50 text-gray-700';
  }

  statusIconClass(status: string): string {
    const map: Record<string, string> = {
      Draft: 'bg-gray-200 text-gray-600',
      Submitted: 'bg-yellow-200 text-yellow-700',
      Approved: 'bg-green-200 text-green-700',
      Rejected: 'bg-red-200 text-red-700',
    };
    return map[status] ?? 'bg-gray-200 text-gray-600';
  }
}
