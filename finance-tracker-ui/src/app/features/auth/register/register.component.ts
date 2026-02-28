import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent {
  form: FormGroup;
  loading = signal(false);
  errorMessage = signal('');

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
  ) {
    this.form = this.fb.group({
      companyName: ['', Validators.required],
      subdomain: [
        '',
        [Validators.required, Validators.pattern('^[a-z0-9-]+$')],
      ],
      adminFirstName: ['', Validators.required],
      adminLastName: ['', Validators.required],
      adminEmail: ['', [Validators.required, Validators.email]],
      password: [
        '',
        [
          Validators.required,
          Validators.minLength(8),
          Validators.pattern('(?=.*[A-Z])(?=.*[0-9]).{8,}'),
        ],
      ],
    });
  }

  onSubmit() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.errorMessage.set('');

    this.authService
      .register({
        companyName: this.form.value.companyName!,
        subdomain: this.form.value.subdomain!,
        adminFirstName: this.form.value.adminFirstName!,
        adminLastName: this.form.value.adminLastName!,
        adminEmail: this.form.value.adminEmail!,
        password: this.form.value.password!,
      })
      .subscribe({
        next: () => this.router.navigate(['/']),
        error: (err) => {
          this.loading.set(false);
          this.errorMessage.set(
            err.error?.title || 'Registration failed. Please try again.',
          );
        },
      });
  }
}
