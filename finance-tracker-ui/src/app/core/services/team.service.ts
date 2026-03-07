import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  TeamMember,
  TeamStats,
  InviteMemberRequest,
} from '../models/team.model';

@Injectable({ providedIn: 'root' })
export class TeamService {
  private readonly url = `${environment.apiUrl}/team`;

  constructor(private http: HttpClient) {}

  getStats(): Observable<TeamStats> {
    return this.http.get<TeamStats>(`${this.url}/stats`);
  }

  getList(
    search?: string,
    role?: string,
    includeInactive = false,
  ): Observable<TeamMember[]> {
    let params = new HttpParams().set('includeInactive', includeInactive);
    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    return this.http.get<TeamMember[]>(this.url, { params });
  }

  getById(id: string): Observable<TeamMember> {
    return this.http.get<TeamMember>(`${this.url}/${id}`);
  }

  invite(payload: InviteMemberRequest): Observable<string> {
    return this.http.post<string>(`${this.url}/invite`, payload);
  }

  changeRole(id: string, role: string): Observable<void> {
    return this.http.put<void>(`${this.url}/${id}/role`, { role });
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/deactivate`, {});
  }

  reactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reactivate`, {});
  }

  resetPassword(id: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.url}/${id}/reset-password`, {
      newPassword,
    });
  }
}
