import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { buildApiUrl } from '../utils/api-url';

export interface MyOrder {
  id: number;
  createdAt: string;
  productId: number;
  productName: string;
  unitPrice: number;
  statusCode: string;
  statusDescription: string;
}

@Injectable({
  providedIn: 'root'
})
export class OrderService {
  constructor(private http: HttpClient) {}

  purchase(productId: number): Observable<{ message: string; orderId: number }> {
    return this.http.post<{ message: string; orderId: number }>(buildApiUrl(`purchases/${productId}`), {});
  }

  getMyOrders(): Observable<MyOrder[]> {
    return this.http.get<MyOrder[]>(buildApiUrl('purchases/my'));
  }
}
