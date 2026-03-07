export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string; // ISO date string — used for token expiry check
  user: UserInfo;
}

export interface UserInfo {
  id: string;
  fullName: string;
  email: string;
  role: 'Employee' | 'Manager' | 'Admin' | 'SuperAdmin';
  tenantId: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  password: string;
}
