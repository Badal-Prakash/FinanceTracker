import { Component, OnInit, signal } from '@angular/core';
import {
  CommonModule,
  CurrencyPipe,
  DatePipe,
  DecimalPipe,
} from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  ReportService,
  ExpenseReportDto,
} from '../../core/services/Report.service';
import { CategoryService } from '../../core/services/category.service';
import { CategoryDto } from '../../core/models/category.model';

type ExportState = 'idle' | 'loading' | 'done' | 'error';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe, DecimalPipe],
  templateUrl: './reports.component.html',
  styleUrls: ['./reports.component.scss'],
})
export class ReportsComponent implements OnInit {
  categories = signal<CategoryDto[]>([]);
  reportData = signal<ExpenseReportDto | null>(null);
  showPreview = signal(false);

  expenseFilters = {
    fromDate: this.firstDayOfMonth(),
    toDate: this.today(),
    status: '',
    categoryId: '',
  };

  budgetMonth = new Date().getMonth() + 1;
  budgetYear = new Date().getFullYear();

  csvState = signal<ExportState>('idle');
  pdfState = signal<ExportState>('idle');
  budgetState = signal<ExportState>('idle');
  previewState = signal<ExportState>('idle');

  readonly statuses = ['Draft', 'Submitted', 'Approved', 'Rejected'];
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
    (_, i) => new Date().getFullYear() - 2 + i,
  );

  constructor(
    private reportService: ReportService,
    private categoryService: CategoryService,
  ) {}

  ngOnInit(): void {
    this.categoryService
      .getAll()
      .subscribe({ next: (c) => this.categories.set(c) });
  }

  // ── Load JSON report + show preview ──────────────────────────────────────
  loadPreview(): void {
    this.previewState.set('loading');
    this.reportService.getExpenseReport(this.activeFilters()).subscribe({
      next: (data) => {
        this.reportData.set(data);
        this.showPreview.set(true);
        this.previewState.set('idle');
      },
      error: () => this.previewState.set('error'),
    });
  }

  // ── Print preview to PDF via browser ─────────────────────────────────────
  printReport(): void {
    this.pdfState.set('loading');
    // Ensure preview is visible before printing
    if (!this.reportData()) {
      this.reportService.getExpenseReport(this.activeFilters()).subscribe({
        next: (data) => {
          this.reportData.set(data);
          this.showPreview.set(true);
          setTimeout(() => {
            window.print();
            this.pdfState.set('idle');
          }, 300);
        },
        error: () => this.pdfState.set('error'),
      });
    } else {
      setTimeout(() => {
        window.print();
        this.pdfState.set('idle');
      }, 100);
    }
  }

  // ── CSV exports ───────────────────────────────────────────────────────────
  exportExpensesCsv(): void {
    this.csvState.set('loading');
    this.reportService.downloadExpensesCsv(this.activeFilters()).subscribe({
      next: (blob) => {
        ReportService.triggerDownload(blob, `expenses_${this.today()}.csv`);
        this.csvState.set('done');
        setTimeout(() => this.csvState.set('idle'), 3000);
      },
      error: () => {
        this.csvState.set('error');
        setTimeout(() => this.csvState.set('idle'), 3000);
      },
    });
  }

  exportBudgetCsv(): void {
    this.budgetState.set('loading');
    this.reportService
      .downloadBudgetCsv(this.budgetMonth, this.budgetYear)
      .subscribe({
        next: (blob) => {
          const label = `${this.budgetYear}_${String(this.budgetMonth).padStart(2, '0')}`;
          ReportService.triggerDownload(blob, `budget_${label}.csv`);
          this.budgetState.set('done');
          setTimeout(() => this.budgetState.set('idle'), 3000);
        },
        error: () => {
          this.budgetState.set('error');
          setTimeout(() => this.budgetState.set('idle'), 3000);
        },
      });
  }

  closePreview(): void {
    this.showPreview.set(false);
  }

  private activeFilters() {
    const f: any = {};
    if (this.expenseFilters.fromDate) f.fromDate = this.expenseFilters.fromDate;
    if (this.expenseFilters.toDate) f.toDate = this.expenseFilters.toDate;
    if (this.expenseFilters.status) f.status = this.expenseFilters.status;
    if (this.expenseFilters.categoryId)
      f.categoryId = this.expenseFilters.categoryId;
    return f;
  }

  today(): string {
    return new Date().toISOString().split('T')[0];
  }

  private firstDayOfMonth(): string {
    const d = new Date();
    return new Date(d.getFullYear(), d.getMonth(), 1)
      .toISOString()
      .split('T')[0];
  }

  monthLabel(m: number): string {
    return this.months.find((x) => x.value === m)?.label ?? '';
  }

  statusClass(status: string): string {
    return `badge badge--${status.toLowerCase()}`;
  }

  barWidth(pct: number): string {
    return `${Math.min(pct, 100)}%`;
  }
}
