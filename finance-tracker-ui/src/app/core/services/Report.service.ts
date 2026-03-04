import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReportFilters {
  fromDate?: string;
  toDate?: string;
  status?: string;
  categoryId?: string;
  userId?: string;
}

export interface CategorySummaryRow {
  categoryName: string;
  categoryColor: string;
  count: number;
  totalAmount: number;
  percentage: number;
}

export interface ExpenseReportRow {
  title: string;
  submittedBy: string;
  category: string;
  amount: number;
  expenseDate: string;
  status: string;
  description: string | null;
  receiptUrl: string | null;
}

export interface ExpenseReportDto {
  totalCount: number;
  totalAmount: number;
  approvedCount: number;
  pendingCount: number;
  categorySummary: CategorySummaryRow[];
  rows: ExpenseReportRow[];
}

@Injectable({ providedIn: 'root' })
export class ReportService {
  private readonly apiUrl = `${environment.apiUrl}/reports`;

  constructor(private http: HttpClient) {}

  private buildParams(filters: ReportFilters): HttpParams {
    let params = new HttpParams();
    Object.entries(filters).forEach(([k, v]) => {
      if (v) params = params.set(k, v);
    });
    return params;
  }

  // Fetch report data as JSON — frontend renders and prints to PDF
  getExpenseReport(filters: ReportFilters): Observable<ExpenseReportDto> {
    return this.http.get<ExpenseReportDto>(`${this.apiUrl}/expenses`, {
      params: this.buildParams(filters),
    });
  }

  // Download expenses CSV
  downloadExpensesCsv(filters: ReportFilters): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/expenses/csv`, {
      params: this.buildParams(filters),
      responseType: 'blob',
    });
  }

  // Download budget CSV
  downloadBudgetCsv(month: number, year: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/budget/csv`, {
      params: new HttpParams().set('month', month).set('year', year),
      responseType: 'blob',
    });
  }

  // Trigger browser file download from a Blob
  static triggerDownload(blob: Blob, fileName: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
  }
}
