import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BudgetSummaryDto,
  BudgetTrendDto,
  SetBudgetRequest,
  CopyBudgetsRequest,
} from '../models/budget.model';

@Injectable({ providedIn: 'root' })
export class BudgetService {
  private readonly apiUrl = `${environment.apiUrl}/budgets`;

  constructor(private http: HttpClient) {}

  getSummary(month?: number, year?: number): Observable<BudgetSummaryDto> {
    let params = new HttpParams();
    if (month) params = params.set('month', month);
    if (year) params = params.set('year', year);
    return this.http.get<BudgetSummaryDto>(`${this.apiUrl}/summary`, {
      params,
    });
  }

  getTrend(months = 6): Observable<BudgetTrendDto[]> {
    return this.http.get<BudgetTrendDto[]>(`${this.apiUrl}/trend`, {
      params: new HttpParams().set('months', months),
    });
  }

  set(request: SetBudgetRequest): Observable<string> {
    return this.http.post<string>(this.apiUrl, request);
  }

  copy(request: CopyBudgetsRequest): Observable<number> {
    return this.http.post<number>(`${this.apiUrl}/copy`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
