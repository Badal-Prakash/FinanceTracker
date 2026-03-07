import {
  Component,
  OnInit,
  OnDestroy,
  signal,
  HostListener,
} from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { interval, Subscription } from 'rxjs';
import { environment } from '../../../../environments/environment';

interface NotificationDto {
  id: string;
  title: string;
  message: string;
  type: string;
  isRead: boolean;
  entityId?: string;
  entityType?: string;
  createdAt: string;
}

interface NotificationSummary {
  unreadCount: number;
  recent: NotificationDto[];
}

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, DatePipe],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss',
})
export class NotificationBellComponent implements OnInit, OnDestroy {
  summary = signal<NotificationSummary>({ unreadCount: 0, recent: [] });
  open = signal(false);
  loading = signal(false);
  private poll?: Subscription;
  private readonly api = `${environment.apiUrl}/notifications`;

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadSummary();
    // Poll every 30 seconds
    this.poll = interval(30_000).subscribe(() => this.loadSummary());
  }

  ngOnDestroy(): void {
    this.poll?.unsubscribe();
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(e: MouseEvent): void {
    const target = e.target as HTMLElement;
    if (!target.closest('.notif-bell')) {
      this.open.set(false);
    }
  }

  loadSummary(): void {
    this.http.get<NotificationSummary>(`${this.api}/summary`).subscribe({
      next: (s) => this.summary.set(s),
    });
  }

  toggleOpen(): void {
    this.open.update((v) => !v);
  }

  markRead(notif: NotificationDto, e: Event): void {
    e.stopPropagation();
    if (notif.isRead) return;
    this.http.post(`${this.api}/${notif.id}/read`, {}).subscribe({
      next: () => {
        this.summary.update((s) => ({
          unreadCount: Math.max(0, s.unreadCount - 1),
          recent: s.recent.map((n) =>
            n.id === notif.id ? { ...n, isRead: true } : n,
          ),
        }));
      },
    });
  }

  markAllRead(): void {
    this.http.post(`${this.api}/read-all`, {}).subscribe({
      next: () => {
        this.summary.update((s) => ({
          unreadCount: 0,
          recent: s.recent.map((n) => ({ ...n, isRead: true })),
        }));
      },
    });
  }

  deleteNotif(notif: NotificationDto, e: Event): void {
    e.stopPropagation();
    this.http.delete(`${this.api}/${notif.id}`).subscribe({
      next: () => {
        this.summary.update((s) => ({
          unreadCount: notif.isRead
            ? s.unreadCount
            : Math.max(0, s.unreadCount - 1),
          recent: s.recent.filter((n) => n.id !== notif.id),
        }));
      },
    });
  }

  navigate(notif: NotificationDto): void {
    this.markRead(notif, new MouseEvent('click'));
    this.open.set(false);
    if (notif.entityType === 'Expense' && notif.entityId)
      this.router.navigate(['/expenses', notif.entityId]);
    else if (notif.entityType === 'Invoice' && notif.entityId)
      this.router.navigate(['/invoices', notif.entityId]);
  }

  viewAll(): void {
    this.open.set(false);
    this.router.navigate(['/notifications']);
  }

  typeIcon(type: string): string {
    const map: Record<string, string> = {
      ExpenseSubmitted: '📋',
      ExpenseApproved: '✅',
      ExpenseRejected: '❌',
      InvoiceOverdue: '⏰',
      InvoicePaid: '💰',
    };
    return map[type] ?? '🔔';
  }

  typeClass(type: string): string {
    const map: Record<string, string> = {
      ExpenseSubmitted: 'type--blue',
      ExpenseApproved: 'type--green',
      ExpenseRejected: 'type--red',
      InvoiceOverdue: 'type--orange',
      InvoicePaid: 'type--green',
    };
    return map[type] ?? '';
  }

  timeAgo(dateStr: string): string {
    const now = Date.now();
    const then = new Date(dateStr).getTime();
    const diff = Math.floor((now - then) / 1000);
    if (diff < 60) return 'just now';
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
  }
}
