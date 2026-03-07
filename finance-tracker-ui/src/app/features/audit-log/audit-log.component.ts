import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface AuditLogDto {
  id: string;
  userId?: string;
  userEmail: string;
  action: string;
  entityName: string;
  entityId: string;
  oldValues?: string;
  newValues?: string;
  changedFields?: string;
  timestamp: string;
  ipAddress?: string;
}

interface AuditLogPageDto {
  items: AuditLogDto[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Component({
  selector: 'app-audit-log',
  standalone: true,
  imports: [CommonModule, FormsModule, DatePipe],
  templateUrl: './audit-log.component.html',
  styleUrl: './audit-log.component.scss',
})
export class AuditLogComponent implements OnInit {
  page = signal<AuditLogPageDto | null>(null);
  loading = signal(true);
  expanded = signal<string | null>(null); // expanded row id

  // Filters
  entityName = '';
  action = '';
  from = '';
  to = '';
  currentPage = 1;
  pageSize = 30;

  readonly entityOptions = ['Expense', 'Invoice', 'Budget', 'Category', 'User'];
  readonly actionOptions = ['Created', 'Updated', 'Deleted'];

  private readonly api = `${environment.apiUrl}/auditlogs`;

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    let params = new HttpParams()
      .set('page', this.currentPage)
      .set('pageSize', this.pageSize);

    if (this.entityName) params = params.set('entityName', this.entityName);
    if (this.action) params = params.set('action', this.action);
    if (this.from)
      params = params.set('from', new Date(this.from).toISOString());
    if (this.to) params = params.set('to', new Date(this.to).toISOString());

    this.http.get<AuditLogPageDto>(this.api, { params }).subscribe({
      next: (p) => {
        this.page.set(p);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.load();
  }

  clearFilters(): void {
    this.entityName = '';
    this.action = '';
    this.from = '';
    this.to = '';
    this.currentPage = 1;
    this.load();
  }

  goTo(p: number): void {
    this.currentPage = p;
    this.load();
  }

  toggleExpand(id: string): void {
    this.expanded.update((v) => (v === id ? null : id));
  }

  parseJson(json?: string): Record<string, unknown> | null {
    if (!json) return null;
    try {
      return JSON.parse(json);
    } catch {
      return null;
    }
  }

  changedFieldsList(log: AuditLogDto): string[] {
    return log.changedFields ? log.changedFields.split(', ') : [];
  }

  actionClass(action: string): string {
    return (
      {
        Created: 'badge--green',
        Updated: 'badge--blue',
        Deleted: 'badge--red',
      }[action] ?? 'badge--grey'
    );
  }

  actionIcon(action: string): string {
    return { Created: '✦', Updated: '✎', Deleted: '✕' }[action] ?? '•';
  }

  get pages(): number[] {
    const total = this.page()?.totalPages ?? 0;
    return Array.from({ length: Math.min(total, 7) }, (_, i) => i + 1);
  }
}
