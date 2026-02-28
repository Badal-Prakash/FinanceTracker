export interface ExpenseListItem {
  id: string;
  title: string;
  amount: number;
  expenseDate: string;
  status: 'Draft' | 'Submitted' | 'Approved' | 'Rejected';
  categoryName: string;
  categoryColor: string;
  submittedByName: string;
  createdAt: string;
}

export interface ExpenseDetail {
  id: string;
  title: string;
  description?: string;
  amount: number;
  expenseDate: string;
  status: string;
  receiptUrl?: string;
  rejectionReason?: string;
  submittedByName: string;
  approverName?: string;
  categoryName: string;
  categoryColor: string;
  createdAt: string;
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateExpenseRequest {
  title: string;
  description?: string;
  amount: number;
  expenseDate: string;
  categoryId: string;
}

export interface Category {
  id: string;
  name: string;
  color: string;
  icon: string;
}

export interface ExpenseFilters {
  page?: number;
  pageSize?: number;
  status?: string;
  categoryId?: string;
  fromDate?: string;
  toDate?: string;
}
