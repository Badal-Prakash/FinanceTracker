import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormArray,
  FormGroup,
  Validators,
} from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { BudgetService } from '../../../core/services/budget.service';
import { CategoryDto } from '../../../core/models/category.model';

// Assumes CategoryService already exists from Categories feature
import { CategoryService } from '../../../core/services/category.service';

@Component({
  selector: 'app-budget-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    RouterLink,
    CurrencyPipe,
  ],
  templateUrl: './budget-form.component.html',
  styleUrls: ['./budget-form.component.scss'],
})
export class BudgetFormComponent implements OnInit {
  categories = signal<CategoryDto[]>([]);
  saving = signal(false);
  copying = signal(false);
  error = signal('');
  success = signal('');

  readonly months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  readonly years = Array.from(
    { length: 5 },
    (_, i) => new Date().getFullYear() - 1 + i,
  );

  // Header controls (not part of formArray)
  selectedMonth = new Date().getMonth() + 1;
  selectedYear = new Date().getFullYear();

  // Copy-from controls
  copyFromMonth = new Date().getMonth() === 0 ? 12 : new Date().getMonth();
  copyFromYear =
    new Date().getMonth() === 0
      ? new Date().getFullYear() - 1
      : new Date().getFullYear();

  // Declared here, initialized in constructor after DI is ready
  form!: ReturnType<FormBuilder['group']>;

  constructor(
    private fb: FormBuilder,
    private budgetService: BudgetService,
    private categoryService: CategoryService,
    private router: Router,
  ) {
    // fb is now available — safe to call here
    this.form = this.fb.group({
      budgets: this.fb.array([]),
    });
  }

  ngOnInit(): void {
    this.categoryService.getAll().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.buildForm(cats);
      },
    });
  }

  get budgetsArray(): FormArray {
    return this.form.get('budgets') as FormArray;
  }

  buildForm(cats: CategoryDto[]): void {
    while (this.budgetsArray.length) this.budgetsArray.removeAt(0);
    cats.forEach((cat) => {
      this.budgetsArray.push(
        this.fb.group({
          categoryId: [cat.id],
          categoryName: [cat.name],
          categoryColor: [cat.color],
          amount: [null, [Validators.min(0)]],
        }),
      );
    });
  }

  rowGroup(i: number): FormGroup {
    return this.budgetsArray.at(i) as FormGroup;
  }

  totalBudget(): number {
    return this.budgetsArray.controls.reduce((sum, ctrl) => {
      return sum + (Number(ctrl.get('amount')?.value) || 0);
    }, 0);
  }

  onSave(): void {
    this.error.set('');
    this.success.set('');

    const rows = this.budgetsArray.value as any[];
    const toSave = rows.filter((r) => r.amount > 0);

    if (!toSave.length) {
      this.error.set('Enter at least one budget amount.');
      return;
    }

    this.saving.set(true);
    let completed = 0;

    toSave.forEach((row) => {
      this.budgetService
        .set({
          categoryId: row.categoryId,
          amount: Number(row.amount),
          month: this.selectedMonth,
          year: this.selectedYear,
        })
        .subscribe({
          next: () => {
            completed++;
            if (completed === toSave.length) {
              this.saving.set(false);
              this.success.set(
                `${completed} budget(s) saved for ${this.monthLabel()}.`,
              );
              setTimeout(() => this.router.navigate(['/budgets']), 1500);
            }
          },
          error: (err) => {
            this.saving.set(false);
            this.error.set(err.error?.title || 'Failed to save budgets.');
          },
        });
    });
  }

  onCopy(): void {
    this.error.set('');
    this.success.set('');
    this.copying.set(true);

    this.budgetService
      .copy({
        fromMonth: this.copyFromMonth,
        fromYear: this.copyFromYear,
        toMonth: this.selectedMonth,
        toYear: this.selectedYear,
      })
      .subscribe({
        next: (count) => {
          this.copying.set(false);
          this.success.set(
            `Copied ${count} budgets from ${this.copyFromLabel()} to ${this.monthLabel()}.`,
          );
          setTimeout(() => this.router.navigate(['/budgets']), 1500);
        },
        error: (err) => {
          this.copying.set(false);
          this.error.set(err.error?.title || 'Failed to copy budgets.');
        },
      });
  }

  monthLabel(): string {
    const m = this.months.find((x) => x.value === this.selectedMonth);
    return `${m?.label ?? ''} ${this.selectedYear}`;
  }

  copyFromLabel(): string {
    const m = this.months.find((x) => x.value === this.copyFromMonth);
    return `${m?.label ?? ''} ${this.copyFromYear}`;
  }
}
