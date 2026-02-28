import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { Category } from '../models/expense.model';

// ─── Models ──────────────────────────────────────────────────────────────────
export interface ExpenseListItem {
  id: string;
  title: string;
  amount: number;
  expenseDate: string;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  categoryName: string;
  categoryColor: string;
  submittedByName: string;
  createdAt: string;
}

export interface ExpenseDetail {
  id: string;
  title: string;
  description?: string;
  amount: number;
  expenseDate: string;
  status: string;
  receiptUrl?: string;
  rejectionReason?: string;
  submittedByName: string;
  approverName?: string;
  categoryName: string;
  categoryColor: string;
  createdAt: string;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateExpenseRequest {
  title: string;
  description?: string;
  amount: number;
  expenseDate: string;
  categoryId: string;
}

export interface ExpenseFilters {
  page?: number;
  pageSize?: number;
  status?: string;
  categoryId?: string;
  fromDate?: string;
  toDate?: string;
}

// ─── Service ──────────────────────────────────────────────────────────────────
@Injectable({ providedIn: 'root' })
export class ExpenseService {
  private readonly apiUrl = `${environment.apiUrl}/expenses`;
  private readonly categoriesUrl = `${environment.apiUrl}/categories`;

  constructor(private http: HttpClient) {}

  getList(filters: ExpenseFilters = {}) {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '')
        params = params.set(key, String(val));
    });
    return this.http.get<PaginatedList<ExpenseListItem>>(this.apiUrl, {
      params,
    });
  }

  getById(id: string) {
    return this.http.get<ExpenseDetail>(`${this.apiUrl}/${id}`);
  }

  create(request: CreateExpenseRequest) {
    return this.http.post<string>(this.apiUrl, request);
  }

  submit(id: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/submit`, {});
  }

  approve(id: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/approve`, {});
  }

  reject(id: string, reason: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/reject`, {
      expenseId: id,
      reason,
    });
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
  getCategories() {
    return this.http.get<Category[]>(this.categoriesUrl);
  }
}
