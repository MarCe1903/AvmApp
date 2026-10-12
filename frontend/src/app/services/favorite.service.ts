import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

@Injectable({
  providedIn: 'root'
})
export class FavoriteService {
  constructor(private http: HttpClient) {}

  getFavorites(): Observable<number[]> {
    return this.http.get<number[]>(buildApiUrl('favorites'));
  }

  addFavorite(productId: number): Observable<void> {
    return this.http.post<void>(buildApiUrl(`favorites/${productId}`), {});
  }

  removeFavorite(productId: number): Observable<void> {
    return this.http.delete<void>(buildApiUrl(`favorites/${productId}`));
  }
}
