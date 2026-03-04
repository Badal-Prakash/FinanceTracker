import { Component, OnInit, signal } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import {
  UserListDto,
  ROLE_LABELS,
  UserRole,
} from '../../../core/models/user.model';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [CommonModule, DatePipe, RouterLink, FormsModule],
  templateUrl: './user-list.component.html',
  styleUrls: ['./user-list.component.scss'],
})
export class UserListComponent implements OnInit {
  users = signal<UserListDto[]>([]);
  loading = signal(true);
  actionLoading = signal<string | null>(null);

  filters = { search: '', role: '', isActive: '' };

  readonly roleLabels = ROLE_LABELS;
  readonly roleOptions: UserRole[] = ['Employee', 'Manager', 'Admin'];

  constructor(private userService: UserService) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const f: any = {};
    if (this.filters.search) f.search = this.filters.search;
    if (this.filters.role) f.role = this.filters.role;
    if (this.filters.isActive) f.isActive = this.filters.isActive === 'true';

    this.userService.getList(f).subscribe({
      next: (u) => {
        this.users.set(u);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  onFilterChange(): void {
    this.load();
  }

  clearFilters(): void {
    this.filters = { search: '', role: '', isActive: '' };
    this.load();
  }

  hasFilters(): boolean {
    return !!(
      this.filters.search ||
      this.filters.role ||
      this.filters.isActive
    );
  }

  deactivate(user: UserListDto): void {
    if (!confirm(`Deactivate ${user.fullName}?`)) return;
    this.actionLoading.set(user.id);
    this.userService.deactivate(user.id).subscribe({
      next: () => {
        this.actionLoading.set(null);
        this.load();
      },
      error: () => this.actionLoading.set(null),
    });
  }

  reactivate(user: UserListDto): void {
    this.actionLoading.set(user.id);
    this.userService.reactivate(user.id).subscribe({
      next: () => {
        this.actionLoading.set(null);
        this.load();
      },
      error: () => this.actionLoading.set(null),
    });
  }

  changeRole(user: UserListDto, newRole: string): void {
    this.actionLoading.set(user.id);
    this.userService.changeRole(user.id, newRole).subscribe({
      next: () => {
        this.actionLoading.set(null);
        this.load();
      },
      error: () => this.actionLoading.set(null),
    });
  }

  roleClass(role: string): string {
    const map: Record<string, string> = {
      Employee: 'badge badge--employee',
      Manager: 'badge badge--manager',
      Admin: 'badge badge--admin',
      SuperAdmin: 'badge badge--superadmin',
    };
    return map[role] ?? 'badge';
  }

  initials(user: UserListDto): string {
    return (user.firstName[0] ?? '') + (user.lastName[0] ?? '');
  }

  activeCount(): number {
    return this.users().filter((u) => u.isActive).length;
  }
  inactiveCount(): number {
    return this.users().filter((u) => !u.isActive).length;
  }
}
