import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-forbidden',
  standalone: true,
  template: `
    <div class="forbidden">
      <div class="forbidden__card">
        <div class="forbidden__code">403</div>
        <h1 class="forbidden__title">Access Denied</h1>
        <p class="forbidden__message">
          You don't have permission to view this page.<br />
          Contact your admin if you think this is a mistake.
        </p>
        <button (click)="goHome()" class="forbidden__btn">
          Go to Dashboard
        </button>
      </div>
    </div>
  `,
  styles: [
    `
      .forbidden {
        min-height: 100vh;
        display: flex;
        align-items: center;
        justify-content: center;
        background: var(--bg-page, #f8fafc);
        padding: 2rem;
      }
      .forbidden__card {
        text-align: center;
        max-width: 400px;
        background: var(--bg-card, #fff);
        border: 1px solid var(--border, #e2e8f0);
        border-radius: 1rem;
        padding: 3rem 2rem;
        box-shadow: 0 4px 24px rgba(0, 0, 0, 0.07);
      }
      .forbidden__code {
        font-size: 5rem;
        font-weight: 800;
        color: #6366f1;
        line-height: 1;
        margin-bottom: 1rem;
      }
      .forbidden__title {
        font-size: 1.5rem;
        font-weight: 700;
        color: var(--text-primary, #0f172a);
        margin: 0 0 0.75rem;
      }
      .forbidden__message {
        color: var(--text-muted, #94a3b8);
        font-size: 0.9375rem;
        line-height: 1.6;
        margin: 0 0 2rem;
      }
      .forbidden__btn {
        padding: 0.625rem 1.5rem;
        background: #6366f1;
        color: #fff;
        border: none;
        border-radius: 0.5rem;
        font-size: 0.9375rem;
        font-weight: 600;
        cursor: pointer;
        &:hover {
          background: #4f46e5;
        }
      }
    `,
  ],
})
export class ForbiddenComponent {
  constructor(private router: Router) {}
  goHome(): void {
    this.router.navigate(['/']);
  }
}
