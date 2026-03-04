export type UserRole = 'Employee' | 'Manager' | 'Admin' | 'SuperAdmin';

export interface UserListDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface UserDetailDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt: string;
}

export interface InviteUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  temporaryPassword: string;
}

export interface UpdateProfileRequest {
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export const ROLES: UserRole[] = ['Employee', 'Manager', 'Admin'];

export const ROLE_LABELS: Record<UserRole, string> = {
  Employee: 'Employee',
  Manager: 'Manager',
  Admin: 'Admin',
  SuperAdmin: 'Super Admin',
};
