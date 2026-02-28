import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormGroup,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ExpenseService } from '../../../core/services/expense.service';
import { Category } from '../../../core/models/expense.model';

@Component({
  selector: 'app-expense-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './expense-form.component.html',
  styleUrl: './expense-form.component.scss',
})
export class ExpenseFormComponent implements OnInit {
  form: FormGroup;
  loading = signal(false);
  errorMessage = signal('');
  categories = signal<Category[]>([]);
  categoriesLoading = signal(true);

  constructor(
    private fb: FormBuilder,
    private expenseService: ExpenseService,
    private router: Router,
  ) {
    // ✅ form initialized inside constructor so fb is ready
    this.form = this.fb.group({
      title: ['', [Validators.required, Validators.maxLength(200)]],
      description: [''],
      amount: [
        null as number | null,
        [Validators.required, Validators.min(0.01)],
      ],
      expenseDate: ['', Validators.required],
      categoryId: ['', Validators.required],
    });
  }

  ngOnInit() {
    // Set today as default date
    const today = new Date().toISOString().split('T')[0];
    this.form.patchValue({ expenseDate: today });

    // Load categories
    this.expenseService.getCategories().subscribe({
      next: (cats: Category[]) => {
        this.categories.set(cats);
        this.categoriesLoading.set(false);
      },
      error: () => this.categoriesLoading.set(false),
    });
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveExpense(true);
  }

  onSubmitDraft() {
    if (this.form.get('title')?.invalid || this.form.get('amount')?.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.saveExpense(false);
  }

  private saveExpense(submitForApproval: boolean) {
    this.loading.set(true);
    this.errorMessage.set('');

    this.expenseService
      .create({
        title: this.form.value.title!,
        description: this.form.value.description || undefined,
        amount: this.form.value.amount!,
        expenseDate: this.form.value.expenseDate!,
        categoryId: this.form.value.categoryId!,
      })
      .subscribe({
        next: (id) => {
          if (submitForApproval) {
            this.expenseService.submit(id).subscribe({
              next: () => this.router.navigate(['/expenses']),
              error: () => this.router.navigate(['/expenses']),
            });
          } else {
            this.router.navigate(['/expenses']);
          }
        },
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(
            err.error?.title || 'Failed to create expense.',
          );
        },
      });
  }
}
