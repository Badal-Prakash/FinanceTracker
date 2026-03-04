import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  Validators,
  FormGroup,
} from '@angular/forms';
import { UserService } from '../../../core/services/user.service';
import { UserDetailDto, ROLE_LABELS } from '../../../core/models/user.model';

@Component({
  selector: 'app-user-profile',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './user-profile.component.html',
  styleUrls: ['./user-profile.component.scss'],
})
export class UserProfileComponent implements OnInit {
  user = signal<UserDetailDto | null>(null);
  loadingProfile = signal(true);
  savingProfile = signal(false);
  savingPassword = signal(false);
  profileSuccess = signal('');
  profileError = signal('');
  passwordSuccess = signal('');
  passwordError = signal('');
  showCurrent = signal(false);
  showNew = signal(false);
  showConfirm = signal(false);
  readonly roleLabels = ROLE_LABELS;
  profileForm!: ReturnType<FormBuilder['group']>;
  passwordForm!: ReturnType<FormBuilder['group']>;
  constructor(
    private fb: FormBuilder,
    private userService: UserService,
  ) {
    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: [
          '',
          [
            Validators.required,
            Validators.minLength(8),
            Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])/),
          ],
        ],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.required, Validators.maxLength(100)]],
    });
  }

  ngOnInit(): void {
    this.userService.getMe().subscribe({
      next: (u) => {
        console.log(u);
        this.user.set(u);
        this.profileForm.patchValue({
          firstName: u.firstName,
          lastName: u.lastName,
        });
        this.loadingProfile.set(false);
      },
      error: () => {
        console.log('error');
        this.loadingProfile.set(false);
      },
    });
  }

  passwordMatchValidator(group: any): { mismatch: boolean } | null {
    const nv = group.get('newPassword')?.value;
    const cv = group.get('confirmPassword')?.value;
    return nv && cv && nv !== cv ? { mismatch: true } : null;
  }

  onSaveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      return;
    }
    this.savingProfile.set(true);
    this.profileError.set('');
    this.profileSuccess.set('');

    this.userService
      .updateProfile({
        firstName: this.profileForm.value.firstName!,
        lastName: this.profileForm.value.lastName!,
      })
      .subscribe({
        next: () => {
          this.savingProfile.set(false);
          this.profileSuccess.set('Profile updated successfully.');
          // Update displayed name
          this.user.update((u) =>
            u
              ? {
                  ...u,
                  firstName: this.profileForm.value.firstName!,
                  lastName: this.profileForm.value.lastName!,
                  fullName: `${this.profileForm.value.firstName} ${this.profileForm.value.lastName}`,
                }
              : u,
          );
        },
        error: (err) => {
          this.savingProfile.set(false);
          this.profileError.set(
            err.error?.title || 'Failed to update profile.',
          );
        },
      });
  }

  onChangePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      return;
    }
    this.savingPassword.set(true);
    this.passwordError.set('');
    this.passwordSuccess.set('');

    this.userService
      .changePassword({
        currentPassword: this.passwordForm.value['currentPassword'],
        newPassword: this.passwordForm.value['newPassword'],
      })
      .subscribe({
        next: () => {
          this.savingPassword.set(false);
          this.passwordSuccess.set('Password changed successfully.');
          this.passwordForm.reset();
        },
        error: (err) => {
          this.savingPassword.set(false);
          this.passwordError.set(
            err.error?.title || err.error || 'Failed to change password.',
          );
        },
      });
  }

  initials(): string {
    const u = this.user();
    return u ? (u.firstName[0] ?? '') + (u.lastName[0] ?? '') : '';
  }

  fieldError(form: any, name: string): string {
    const ctrl = form.get(name);
    if (!ctrl?.invalid || !ctrl.touched) return '';
    if (ctrl.errors?.['required']) return 'This field is required.';
    if (ctrl.errors?.['minlength']) return 'Minimum 8 characters required.';
    if (ctrl.errors?.['pattern'])
      return 'Must contain an uppercase letter and a number.';
    return 'Invalid value.';
  }
}
