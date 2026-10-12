import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { UserListItem } from '../models/user';
import { buildApiUrl } from '../utils/api-url';

@Injectable({
  providedIn: 'root'
})
export class UserService {
  constructor(private http: HttpClient) {}

  getUsers(): Observable<UserListItem[]> {
    return this.http.get<UserListItem[]>(buildApiUrl('users'));
  }

  updateRole(userId: number, role: string): Observable<void> {
    return this.http.put<void>(buildApiUrl(`users/${userId}/role`), { role });
  }

  deleteUser(userId: number): Observable<void> {
    return this.http.delete<void>(buildApiUrl(`users/${userId}`));
  }
}
