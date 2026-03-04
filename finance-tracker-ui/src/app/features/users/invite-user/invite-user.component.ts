import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormGroup,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { UserService } from '../../../core/services/user.service';
import { ROLES, ROLE_LABELS } from '../../../core/models/user.model';

@Component({
  selector: 'app-invite-user',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './invite-user.component.html',
  styleUrls: ['./invite-user.component.scss'],
})
export class InviteUserComponent {
  loading = signal(false);
  error = signal('');
  showPassword = signal(false);
  form: FormGroup;

  readonly roles = ROLES;
  readonly roleLabels = ROLE_LABELS;

  constructor(
    private fb: FormBuilder,
    private userService: UserService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email]],
      role: ['Employee', Validators.required],
      temporaryPassword: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])/),
        ],
      ],
    });
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set('');

    this.userService
      .invite({
        firstName: this.form.value.firstName!,
        lastName: this.form.value.lastName!,
        email: this.form.value.email!,
        role: this.form.value.role!,
        temporaryPassword: this.form.value.temporaryPassword!,
      })
      .subscribe({
        next: () => this.router.navigate(['/users']),
        error: (err) => {
          this.loading.set(false);
          this.error.set(
            err.error?.title || err.error || 'Failed to invite user.',
          );
        },
      });
  }

  fieldError(name: string): string {
    const ctrl = this.form.get(name);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.errors?.['required']) return 'This field is required.';
    if (ctrl.errors?.['email']) return 'Enter a valid email address.';
    if (ctrl.errors?.['minlength']) return 'Minimum 8 characters required.';
    if (ctrl.errors?.['maxlength']) return 'Too long.';
    if (ctrl.errors?.['pattern'])
      return 'Must contain an uppercase letter and a number.';
    return 'Invalid value.';
  }
}
