import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
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

@Component({
  selector: 'app-notifications-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notifications-page.component.html',
  styleUrl: './notifications-page.component.scss',
})
export class NotificationsPageComponent implements OnInit {
  notifications = signal<NotificationDto[]>([]);
  loading = signal(true);
  filter = signal<'all' | 'unread'>('all');
  private readonly api = `${environment.apiUrl}/notifications`;

  constructor(
    private http: HttpClient,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const unreadOnly = this.filter() === 'unread';
    this.http
      .get<
        NotificationDto[]
      >(`${this.api}?unreadOnly=${unreadOnly}&pageSize=50`)
      .subscribe({
        next: (n) => {
          this.notifications.set(n);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  setFilter(f: 'all' | 'unread'): void {
    this.filter.set(f);
    this.load();
  }

  markRead(notif: NotificationDto): void {
    if (notif.isRead) return;
    this.http.post(`${this.api}/${notif.id}/read`, {}).subscribe({
      next: () =>
        this.notifications.update((list) =>
          list.map((n) => (n.id === notif.id ? { ...n, isRead: true } : n)),
        ),
    });
  }

  markAllRead(): void {
    this.http.post(`${this.api}/read-all`, {}).subscribe({
      next: () =>
        this.notifications.update((list) =>
          list.map((n) => ({ ...n, isRead: true })),
        ),
    });
  }

  delete(notif: NotificationDto, e: Event): void {
    e.stopPropagation();
    this.http.delete(`${this.api}/${notif.id}`).subscribe({
      next: () =>
        this.notifications.update((list) =>
          list.filter((n) => n.id !== notif.id),
        ),
    });
  }

  navigate(notif: NotificationDto): void {
    this.markRead(notif);
    if (notif.entityType === 'Expense' && notif.entityId)
      this.router.navigate(['/expenses', notif.entityId]);
    else if (notif.entityType === 'Invoice' && notif.entityId)
      this.router.navigate(['/invoices', notif.entityId]);
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
    const diff = Math.floor((Date.now() - new Date(dateStr).getTime()) / 1000);
    if (diff < 60) return 'just now';
    if (diff < 3600) return `${Math.floor(diff / 60)}m ago`;
    if (diff < 86400) return `${Math.floor(diff / 3600)}h ago`;
    return `${Math.floor(diff / 86400)}d ago`;
  }

  get unreadCount(): number {
    return this.notifications().filter((n) => !n.isRead).length;
  }
}
