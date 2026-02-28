import { Component, computed, Signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, RouterOutlet, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { UserInfo } from '../../core/models/auth.model';
import { ThemeService } from '../../core/services/theme.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterLink, RouterOutlet, RouterLinkActive],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  user!: Signal<UserInfo | null>;
  userInitials!: Signal<string>;
  isDark!: Signal<boolean>;

  constructor(
    private authService: AuthService,
    private themeService: ThemeService,
  ) {
    this.user = this.authService.currentUser;
    this.isDark = this.themeService.isDark;

    this.userInitials = computed(() => {
      const name = this.user()?.fullName ?? '';
      return name
        .split(' ')
        .map((n) => n[0])
        .join('')
        .toUpperCase()
        .slice(0, 2);
    });
  }

  toggleTheme() {
    this.themeService.toggle();
  }

  logout() {
    this.authService.logout();
  }
}
