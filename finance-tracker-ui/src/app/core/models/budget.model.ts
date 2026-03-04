export interface BudgetDto {
  id: string;
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  categoryIcon: string;
  budgetedAmount: number;
  spentAmount: number;
  remainingAmount: number;
  utilisationPercent: number;
  alertLevel: 'ok' | 'warning' | 'critical' | 'exceeded';
  month: number;
  year: number;
}

export interface BudgetAlertDto {
  categoryName: string;
  categoryColor: string;
  budgetedAmount: number;
  spentAmount: number;
  utilisationPercent: number;
  level: 'warning' | 'critical' | 'exceeded';
}

export interface BudgetSummaryDto {
  month: number;
  year: number;
  monthLabel: string;
  totalBudgeted: number;
  totalSpent: number;
  totalRemaining: number;
  overallUtilisationPercent: number;
  categories: BudgetDto[];
  alerts: BudgetAlertDto[];
}

export interface BudgetTrendDto {
  month: number;
  year: number;
  monthLabel: string;
  totalBudgeted: number;
  totalSpent: number;
  utilisationPercent: number;
}

export interface SetBudgetRequest {
  categoryId: string;
  amount: number;
  month: number;
  year: number;
}

export interface CopyBudgetsRequest {
  fromMonth: number;
  fromYear: number;
  toMonth: number;
  toYear: number;
}
