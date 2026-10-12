import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response';
import { Product } from '../models/product';
import { ProductDetail } from '../models/product-detail';
import { buildApiUrl } from '../utils/api-url';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Product[]>> {
    return this.http.get<ApiResponse<Product[]>>(buildApiUrl('products/getall'));
  }

  getDetails(): Observable<ApiResponse<ProductDetail[]>> {
    return this.http.get<ApiResponse<ProductDetail[]>>(buildApiUrl('products/getproductdetails'));
  }

  getById(id: number): Observable<ApiResponse<Product>> {
    return this.http.get<ApiResponse<Product>>(buildApiUrl(`products/getbyid?id=${id}`));
  }

  add(product: Product): Observable<ApiResponse<null>> {
    return this.http.post<ApiResponse<null>>(buildApiUrl('products/add'), product);
  }

  update(product: Product): Observable<ApiResponse<null>> {
    return this.http.put<ApiResponse<null>>(buildApiUrl('products/update'), product);
  }

  delete(id: number): Observable<ApiResponse<null>> {
    return this.http.delete<ApiResponse<null>>(buildApiUrl(`products/${id}`));
  }
}
