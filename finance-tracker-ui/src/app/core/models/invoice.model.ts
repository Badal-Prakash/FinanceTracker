export interface InvoiceLineItem {
  id?: string;
  description: string;
  quantity: number;
  unitPrice: number;
  total?: number;
}

export interface InvoiceListItem {
  id: string;
  invoiceNumber: string;
  clientName: string;
  amount: number;
  dueDate: string;
  status: 'Draft' | 'Unpaid' | 'Paid' | 'Overdue' | 'Cancelled';
  paidAt?: string;
  createdAt: string;
  clientEmail: string;
}

export interface InvoiceDetail {
  id: string;
  invoiceNumber: string;
  clientName: string;
  clientEmail: string;
  clientAddress?: string;
  amount: number;
  dueDate: string;
  status: string;
  paidAt?: string;
  notes?: string;
  pdfUrl?: string;
  lineItems: InvoiceLineItem[];
  createdAt: string;
}

export interface InvoiceStats {
  totalUnpaid: number;
  totalPaidThisMonth: number;
  totalOverdue: number;
  unpaidCount: number;
  overdueCount: number;
  paidThisMonthCount: number;
}

export interface PaginatedInvoices {
  items: InvoiceListItem[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface CreateInvoiceRequest {
  clientName: string;
  clientEmail: string;
  clientAddress?: string;
  dueDate: string;
  notes?: string;
  lineItems: { description: string; quantity: number; unitPrice: number }[];
}

export interface InvoiceFilters {
  page?: number;
  pageSize?: number;
  status?: string;
  clientName?: string;
  fromDate?: string;
  toDate?: string;
}
