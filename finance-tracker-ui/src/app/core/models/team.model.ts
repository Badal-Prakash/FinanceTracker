export interface TeamMember {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  role: 'Employee' | 'Manager' | 'Admin' | 'SuperAdmin';
  isActive: boolean;
  createdAt: string;
  totalExpenses: number;
  totalExpenseAmount: number;
  pendingExpenses: number;
}

export interface TeamStats {
  totalMembers: number;
  activeMembers: number;
  employees: number;
  managers: number;
  admins: number;
}

export interface InviteMemberRequest {
  firstName: string;
  lastName: string;
  email: string;
  role: string;
  temporaryPassword: string;
}
