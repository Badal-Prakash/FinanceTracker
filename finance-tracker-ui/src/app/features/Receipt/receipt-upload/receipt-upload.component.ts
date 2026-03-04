import {
  Component,
  Input,
  Output,
  EventEmitter,
  signal,
  computed,
  HostListener,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReceiptService } from '../../../core/services/receipt.service';

export interface ReceiptState {
  url: string | null;
  fileName?: string;
}

@Component({
  selector: 'app-receipt-upload',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './receipt-upload.component.html',
  styleUrls: ['./receipt-upload.component.scss'],
})
export class ReceiptUploadComponent {
  @Input() expenseId!: string;
  @Input() existingUrl: string | null = null;
  @Input() disabled = false;

  @Output() uploaded = new EventEmitter<ReceiptState>();
  @Output() removed = new EventEmitter<void>();

  isDragOver = signal(false);
  uploading = signal(false);
  removing = signal(false);
  uploadPercent = signal(0);
  error = signal('');

  // Local preview URL (data URL for images, null for PDFs)
  previewUrl = signal<string | null>(null);
  previewName = signal<string | null>(null);

  // Current receipt url (may come from @Input or after upload)
  currentUrl = computed(() => this.previewUrl() ?? this.existingUrl);

  readonly allowedTypes = [
    'image/jpeg',
    'image/png',
    'image/webp',
    'image/heic',
    'application/pdf',
  ];
  readonly maxSize = 10 * 1024 * 1024; // 10 MB

  constructor(private receiptService: ReceiptService) {}

  // ── Drag and drop ──────────────────────────────────────────────────────────
  @HostListener('dragover', ['$event'])
  onDragOver(e: DragEvent): void {
    e.preventDefault();
    if (!this.disabled && !this.uploading()) this.isDragOver.set(true);
  }

  @HostListener('dragleave', ['$event'])
  onDragLeave(e: DragEvent): void {
    e.preventDefault();
    this.isDragOver.set(false);
  }

  @HostListener('drop', ['$event'])
  onDrop(e: DragEvent): void {
    e.preventDefault();
    this.isDragOver.set(false);
    if (this.disabled || this.uploading()) return;
    const file = e.dataTransfer?.files?.[0];
    if (file) this.processFile(file);
  }

  // ── File input ─────────────────────────────────────────────────────────────
  onFileSelected(e: Event): void {
    const input = e.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.processFile(file);
    input.value = ''; // Reset so same file can be re-selected
  }

  private processFile(file: File): void {
    this.error.set('');

    if (!this.allowedTypes.includes(file.type)) {
      this.error.set('Invalid file type. Allowed: JPG, PNG, WebP, HEIC, PDF.');
      return;
    }

    if (file.size > this.maxSize) {
      this.error.set('File is too large. Maximum size is 10 MB.');
      return;
    }

    // Show local preview for images
    if (file.type.startsWith('image/')) {
      const reader = new FileReader();
      reader.onload = () => this.previewUrl.set(reader.result as string);
      reader.readAsDataURL(file);
    } else {
      this.previewUrl.set(null);
    }
    this.previewName.set(file.name);

    this.uploadFile(file);
  }

  private uploadFile(file: File): void {
    this.uploading.set(true);
    this.uploadPercent.set(0);

    this.receiptService.upload(this.expenseId, file).subscribe({
      next: (progress) => {
        this.uploadPercent.set(progress.percent);
        if (progress.done && progress.result) {
          this.uploading.set(false);
          this.uploaded.emit({
            url: progress.result.url,
            fileName: progress.result.fileName,
          });
        }
      },
      error: (err) => {
        this.uploading.set(false);
        this.previewUrl.set(null);
        this.previewName.set(null);
        this.error.set(err.error?.title || 'Upload failed. Please try again.');
      },
    });
  }

  removeReceipt(): void {
    if (!confirm('Remove the attached receipt?')) return;
    this.removing.set(true);
    this.receiptService.remove(this.expenseId).subscribe({
      next: () => {
        this.removing.set(false);
        this.previewUrl.set(null);
        this.previewName.set(null);
        this.removed.emit();
      },
      error: (err) => {
        this.removing.set(false);
        this.error.set(err.error?.title || 'Could not remove receipt.');
      },
    });
  }

  isPdf(url: string | null): boolean {
    return !!url && (url.endsWith('.pdf') || url.includes('.pdf?'));
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }
}
