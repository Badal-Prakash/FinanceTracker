import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
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
  private readonly apiUrl = `${environment.apiUrl}/invoices`;

  constructor(private http: HttpClient) {}

  getList(filters: InvoiceFilters = {}) {
    let params = new HttpParams();
    Object.entries(filters).forEach(([key, val]) => {
      if (val !== undefined && val !== null && val !== '')
        params = params.set(key, String(val));
    });
    return this.http.get<PaginatedInvoices>(this.apiUrl, { params });
  }

  getById(id: string) {
    return this.http.get<InvoiceDetail>(`${this.apiUrl}/${id}`);
  }

  getStats() {
    return this.http.get<InvoiceStats>(`${this.apiUrl}/stats`);
  }

  create(request: CreateInvoiceRequest) {
    return this.http.post<string>(this.apiUrl, request);
  }

  update(id: string, request: CreateInvoiceRequest) {
    return this.http.put<void>(`${this.apiUrl}/${id}`, request);
  }

  send(id: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/send`, {});
  }

  markPaid(id: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/mark-paid`, {});
  }

  cancel(id: string) {
    return this.http.post<void>(`${this.apiUrl}/${id}/cancel`, {});
  }

  delete(id: string) {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
