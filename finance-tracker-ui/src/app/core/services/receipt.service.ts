import { Injectable } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, map, filter } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ReceiptUploadResult {
  url: string;
  fileName: string;
  sizeBytes: number;
}

export interface UploadProgress {
  percent: number;
  done: boolean;
  result?: ReceiptUploadResult;
}

@Injectable({ providedIn: 'root' })
export class ReceiptService {
  private readonly apiUrl = `${environment.apiUrl}/expenses`;

  constructor(private http: HttpClient) {}

  // Upload with progress tracking
  upload(expenseId: string, file: File): Observable<UploadProgress> {
    const formData = new FormData();
    formData.append('file', file);

    const req = new HttpRequest(
      'POST',
      `${this.apiUrl}/${expenseId}/receipt`,
      formData,
      { reportProgress: true },
    );

    return this.http.request(req).pipe(
      filter(
        (e) =>
          e.type === HttpEventType.UploadProgress ||
          e.type === HttpEventType.Response,
      ),
      map((e) => {
        if (e.type === HttpEventType.UploadProgress) {
          const percent = e.total ? Math.round((100 * e.loaded) / e.total) : 0;
          return { percent, done: false };
        }
        // Response
        const body = (e as any).body as ReceiptUploadResult;
        return { percent: 100, done: true, result: body };
      }),
    );
  }

  // Simple upload (no progress)
  remove(expenseId: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${expenseId}/receipt`);
  }
}
