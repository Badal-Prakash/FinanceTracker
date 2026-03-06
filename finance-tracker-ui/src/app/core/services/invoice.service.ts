import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  InvoiceListItem,
  InvoiceDetail,
  InvoiceStats,
  PaginatedInvoices,
  CreateInvoiceRequest,
  InvoiceFilters,
} from '../models/invoice.model';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly url = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<InvoiceStats> {
    return this.http.get<InvoiceStats>(`${this.url}/stats`);
  }

  getList(filters: InvoiceFilters = {}): Observable<PaginatedInvoices> {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 20);
    if (filters.status) params = params.set('status', filters.status);
    if (filters.clientName)
      params = params.set('clientName', filters.clientName);
    if (filters.fromDate) params = params.set('fromDate', filters.fromDate);
    if (filters.toDate) params = params.set('toDate', filters.toDate);
    return this.http.get<PaginatedInvoices>(this.url, { params });
  }

  getById(id: string): Observable<InvoiceDetail> {
    return this.http.get<InvoiceDetail>(`${this.url}/${id}`);
  }

  create(payload: CreateInvoiceRequest): Observable<string> {
    return this.http.post<string>(this.url, payload);
  }

  update(id: string, payload: CreateInvoiceRequest): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}`, payload);
  }

  markPaid(id: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/mark-paid`, {});
  }

  cancel(id: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/cancel`, {});
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
