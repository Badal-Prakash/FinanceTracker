import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamService } from '../../../core/services/team.service';
import { TeamMember, TeamStats } from '../../../core/models/team.model';

type Modal = 'invite' | 'role' | 'password' | null;

@Component({
  selector: 'app-team-list',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe],
  templateUrl: './team-list.component.html',
  styleUrl: './team-list.component.scss',
})
export class TeamListComponent implements OnInit {
  members = signal<TeamMember[]>([]);
  stats = signal<TeamStats | null>(null);
  loading = signal(true);
  acting = signal(false);
  error = signal('');
  modal = signal<Modal>(null);
  selectedMember = signal<TeamMember | null>(null);

  // Filters
  search = '';
  roleFilter = '';
  includeInactive = false;

  // Invite form
  invite = {
    firstName: '',
    lastName: '',
    email: '',
    role: 'Employee',
    temporaryPassword: '',
  };
  inviteError = signal('');
  inviteSaving = signal(false);

  // Change role form
  newRole = '';

  // Reset password form
  newPassword = '';
  confirmPassword = '';
  passwordError = signal('');
  showPassword = signal(false);

  readonly roles = ['Employee', 'Manager', 'Admin'];

  constructor(private teamService: TeamService) {}

  ngOnInit(): void {
    this.loadStats();
    this.loadMembers();
  }

  loadStats(): void {
    this.teamService.getStats().subscribe({ next: (s) => this.stats.set(s) });
  }

  loadMembers(): void {
    this.loading.set(true);
    this.teamService
      .getList(
        this.search || undefined,
        this.roleFilter || undefined,
        this.includeInactive,
      )
      .subscribe({
        next: (m) => {
          this.members.set(m);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  onSearch(): void {
    this.loadMembers();
  }
  onFilter(): void {
    this.loadMembers();
  }

  // ── Invite ──────────────────────────────────────────────────────────────────
  openInvite(): void {
    this.invite = {
      firstName: '',
      lastName: '',
      email: '',
      role: 'Employee',
      temporaryPassword: '',
    };
    this.inviteError.set('');
    this.modal.set('invite');
  }

  submitInvite(): void {
    this.inviteError.set('');
    const { firstName, lastName, email, role, temporaryPassword } = this.invite;
    if (!firstName.trim() || !lastName.trim()) {
      this.inviteError.set('First and last name are required.');
      return;
    }
    if (!email.trim()) {
      this.inviteError.set('Email is required.');
      return;
    }
    if (temporaryPassword.length < 8) {
      this.inviteError.set('Password must be at least 8 characters.');
      return;
    }

    this.inviteSaving.set(true);
    this.teamService
      .invite({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim(),
        role,
        temporaryPassword,
      })
      .subscribe({
        next: () => {
          this.inviteSaving.set(false);
          this.modal.set(null);
          this.loadMembers();
          this.loadStats();
        },
        error: (err) => {
          this.inviteError.set(
            err?.error?.message ?? 'Failed to invite member.',
          );
          this.inviteSaving.set(false);
        },
      });
  }

  // ── Change Role ─────────────────────────────────────────────────────────────
  openChangeRole(member: TeamMember, e: Event): void {
    e.stopPropagation();
    this.selectedMember.set(member);
    this.newRole = member.role;
    this.modal.set('role');
  }

  submitChangeRole(): void {
    const member = this.selectedMember();
    if (!member) return;
    this.acting.set(true);
    this.teamService.changeRole(member.id, this.newRole).subscribe({
      next: () => {
        this.acting.set(false);
        this.modal.set(null);
        this.loadMembers();
      },
      error: () => this.acting.set(false),
    });
  }

  // ── Deactivate / Reactivate ─────────────────────────────────────────────────
  toggleActive(member: TeamMember, e: Event): void {
    e.stopPropagation();
    const action = member.isActive ? 'deactivate' : 'reactivate';
    if (
      !confirm(
        `${action.charAt(0).toUpperCase() + action.slice(1)} ${member.fullName}?`,
      )
    )
      return;
    this.acting.set(true);
    const req$ = member.isActive
      ? this.teamService.deactivate(member.id)
      : this.teamService.reactivate(member.id);
    req$.subscribe({
      next: () => {
        this.acting.set(false);
        this.loadMembers();
        this.loadStats();
      },
      error: () => this.acting.set(false),
    });
  }

  // ── Reset Password ──────────────────────────────────────────────────────────
  openResetPassword(member: TeamMember, e: Event): void {
    e.stopPropagation();
    this.selectedMember.set(member);
    this.newPassword = '';
    this.confirmPassword = '';
    this.passwordError.set('');
    this.modal.set('password');
  }

  submitResetPassword(): void {
    this.passwordError.set('');
    if (this.newPassword.length < 8) {
      this.passwordError.set('Password must be at least 8 characters.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('Passwords do not match.');
      return;
    }

    const member = this.selectedMember();
    if (!member) return;
    this.acting.set(true);
    this.teamService.resetPassword(member.id, this.newPassword).subscribe({
      next: () => {
        this.acting.set(false);
        this.modal.set(null);
      },
      error: (err) => {
        this.passwordError.set(
          err?.error?.message ?? 'Failed to reset password.',
        );
        this.acting.set(false);
      },
    });
  }

  closeModal(): void {
    this.modal.set(null);
  }

  toggleShowPassword(): void {
    this.showPassword.update((v) => !v);
  }

  roleClass(role: string): string {
    const m: Record<string, string> = {
      Employee: 'role--employee',
      Manager: 'role--manager',
      Admin: 'role--admin',
      SuperAdmin: 'role--superadmin',
    };
    return 'role-badge ' + (m[role] ?? '');
  }

  initials(member: TeamMember): string {
    return (member.firstName[0] + member.lastName[0]).toUpperCase();
  }

  avatarColor(name: string): string {
    const colors = [
      '#6366f1',
      '#8b5cf6',
      '#ec4899',
      '#f59e0b',
      '#10b981',
      '#3b82f6',
      '#ef4444',
      '#14b8a6',
    ];
    let hash = 0;
    for (const c of name) hash = c.charCodeAt(0) + ((hash << 5) - hash);
    return colors[Math.abs(hash) % colors.length];
  }
}
