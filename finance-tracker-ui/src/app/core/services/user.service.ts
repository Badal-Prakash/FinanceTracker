import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  UserListDto,
  UserDetailDto,
  InviteUserRequest,
  UpdateProfileRequest,
  ChangePasswordRequest,
} from '../models/user.model';

@Injectable({ providedIn: 'root' })
export class UserService {
  private readonly apiUrl = `${environment.apiUrl}/users`;

  constructor(private http: HttpClient) {}

  getList(
    filters: { search?: string; role?: string; isActive?: boolean } = {},
  ): Observable<UserListDto[]> {
    let params = new HttpParams();
    if (filters.search) params = params.set('search', filters.search);
    if (filters.role) params = params.set('role', filters.role);
    if (filters.isActive !== undefined)
      params = params.set('isActive', filters.isActive);
    return this.http.get<UserListDto[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<UserDetailDto> {
    return this.http.get<UserDetailDto>(`${this.apiUrl}/${id}`);
  }

  getMe(): Observable<UserDetailDto> {
    return this.http.get<UserDetailDto>(`${this.apiUrl}/me`);
  }

  invite(request: InviteUserRequest): Observable<string> {
    return this.http.post<string>(`${this.apiUrl}/invite`, request);
  }

  changeRole(id: string, role: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}/role`, { role });
  }

  deactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  reactivate(id: string): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/reactivate`, {});
  }

  updateProfile(request: UpdateProfileRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/me/profile`, request);
  }

  changePassword(request: ChangePasswordRequest): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/me/password`, request);
  }
}
