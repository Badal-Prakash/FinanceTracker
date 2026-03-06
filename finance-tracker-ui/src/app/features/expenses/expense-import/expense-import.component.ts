import {
  Component,
  EventEmitter,
  Output,
  signal,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface ImportPreviewRow {
  title: string;
  amount: number;
  expenseDate: string;
  categoryName: string;
  description?: string;
  isValid: boolean;
  errors?: string[];
}

@Component({
  selector: 'app-expense-import',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './expense-import.component.html',
  styleUrl: './expense-import.component.scss',
})
export class ExpenseImportComponent {
  @Output() closed = new EventEmitter<boolean>();
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  loading = signal(false);
  importing = signal(false);
  previewData = signal<ImportPreviewRow[]>([]);
  error = signal<string | null>(null);
  file = signal<File | null>(null);

  constructor(private http: HttpClient) {}

  close(): void {
    this.closed.emit(false);
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFile(input.files[0]);
    }
  }

  onFileDropped(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer?.files && event.dataTransfer.files.length > 0) {
      this.handleFile(event.dataTransfer.files[0]);
    }
  }

  private handleFile(file: File): void {
    const ext = file.name.split('.').pop()?.toLowerCase();
    if (ext !== 'csv' && ext !== 'xlsx') {
      this.error.set('Please upload a .csv or .xlsx file.');
      return;
    }
    this.file.set(file);
    this.uploadForPreview(file);
  }

  uploadForPreview(file: File): void {
    this.loading.set(true);
    this.error.set(null);

    const formData = new FormData();
    formData.append('file', file);

    this.http
      .post<
        ImportPreviewRow[]
      >(`${environment.apiUrl}/expenses/import/preview`, formData)
      .subscribe({
        next: (data) => {
          this.previewData.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          const message =
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            'Failed to preview file. Please check the file format and try again.';
          this.error.set(message);
          this.loading.set(false);
        },
      });
  }

  confirmImport(): void {
    if (!this.file()) return;
    this.importing.set(true);
    this.error.set(null);
    const formData = new FormData();
    formData.append('file', this.file()!);

    this.http
      .post(`${environment.apiUrl}/expenses/import`, formData)
      .subscribe({
        next: () => {
          this.importing.set(false);
          this.closed.emit(true);
        },
        error: (err) => {
          const message =
            err.error?.message ||
            (typeof err.error === 'string' ? err.error : null) ||
            'An unexpected error occurred during import.';
          this.error.set(message);
          this.importing.set(false);
        },
      });
  }

  downloadTemplate(): void {
    const rows = [
      'Title,Amount,Date,Category,Description',
      'Lunch,15.00,2023-12-01,Food,Team Lunch',
    ];
    const csvContent = rows.join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = 'import_template.csv';
    link.click();
    window.URL.revokeObjectURL(url);
  }
}
