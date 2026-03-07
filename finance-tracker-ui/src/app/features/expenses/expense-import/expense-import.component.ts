import {
  Component,
  signal,
  computed,
  EventEmitter,
  Output,
} from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface ImportRow {
  rowNumber: number;
  title: string;
  description: string;
  amount: number;
  expenseDate: string;
  category: string;
  isValid: boolean;
  error?: string;
}

interface ImportPreview {
  rows: ImportRow[];
  validCount: number;
  errorCount: number;
  availableCategories: string[];
}

interface ImportResult {
  imported: number;
  skipped: number;
  errors: string[];
}

type Step = 'upload' | 'preview' | 'done';

@Component({
  selector: 'app-expense-import',
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe],
  templateUrl: './expense-import.component.html',
  styleUrl: './expense-import.component.scss',
})
export class ExpenseImportComponent {
  @Output() closed = new EventEmitter<boolean>(); // true = imported successfully

  step = signal<Step>('upload');
  dragging = signal(false);
  uploading = signal(false);
  importing = signal(false);
  preview = signal<ImportPreview | null>(null);
  result = signal<ImportResult | null>(null);
  error = signal('');
  submitAfterImport = false;
  skipErrors = true;
  selectedFile: File | null = null;

  showOnlyErrors = signal(false);

  visibleRows = computed(() => {
    const rows = this.preview()?.rows ?? [];
    return this.showOnlyErrors() ? rows.filter((r) => !r.isValid) : rows;
  });

  private readonly api = `${environment.apiUrl}/expenses`;

  onDragOver(e: DragEvent) {
    e.preventDefault();
    this.dragging.set(true);
  }
  onDragLeave() {
    this.dragging.set(false);
  }
  onDrop(e: DragEvent) {
    e.preventDefault();
    this.dragging.set(false);
    const f = e.dataTransfer?.files[0];
    if (f) this.handleFile(f);
  }
  onFileSelect(e: Event) {
    const f = (e.target as HTMLInputElement).files?.[0];
    if (f) this.handleFile(f);
  }

  handleFile(file: File): void {
    const ext = file.name.split('.').pop()?.toLowerCase();
    if (!['csv', 'xlsx'].includes(ext ?? '')) {
      this.error.set('Only CSV and Excel (.xlsx) files are supported.');
      return;
    }
    this.error.set('');
    this.selectedFile = file;
    this.uploadForPreview();
  }

  uploadForPreview(): void {
    if (!this.selectedFile) return;
    this.uploading.set(true);
    this.error.set('');

    const form = new FormData();
    form.append('file', this.selectedFile);

    this.http
      .post<ImportPreview>(`${this.api}/import/preview`, form)
      .subscribe({
        next: (data) => {
          this.preview.set(data);
          this.step.set('preview');
          this.uploading.set(false);
        },
        error: (err) => {
          this.error.set(
            err?.error?.message ??
              'Failed to parse file. Check the format and try again.',
          );
          this.uploading.set(false);
        },
      });
  }

  confirmImport(): void {
    if (!this.selectedFile) return;
    this.importing.set(true);

    const form = new FormData();
    form.append('file', this.selectedFile);
    form.append('submitAfterImport', String(this.submitAfterImport));
    form.append('skipErrors', String(this.skipErrors));

    this.http.post<ImportResult>(`${this.api}/import`, form).subscribe({
      next: (res) => {
        this.result.set(res);
        this.step.set('done');
        this.importing.set(false);
      },
      error: (err) => {
        this.error.set(
          err?.error?.message ?? 'Import failed. Please try again.',
        );
        this.importing.set(false);
      },
    });
  }

  downloadTemplate(): void {
    this.http
      .get(`${this.api}/import/template`, { responseType: 'blob' })
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = 'expense-import-template.csv';
          a.click();
          URL.revokeObjectURL(url);
        },
      });
  }

  reset(): void {
    this.step.set('upload');
    this.preview.set(null);
    this.result.set(null);
    this.error.set('');
    this.selectedFile = null;
  }

  close(success = false): void {
    this.closed.emit(success);
  }

  constructor(private http: HttpClient) {}
}
