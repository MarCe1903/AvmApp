import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ProductService } from '../../services/product.service';
import { Product } from '../../models/product';
import { ApiResponse } from '../../models/api-response';
import { finalize, timeout } from 'rxjs';
import { extractArray } from '../../utils/response-array';

@Component({
  selector: 'app-seller-panel',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './seller-panel.component.html',
  styleUrls: ['./seller-panel.component.css']
})
export class SellerPanelComponent implements OnInit {
  products: Product[] = [];
  loading = false;
  listError: string | null = null;
  formError: string | null = null;
  saving = false;
  success: string | null = null;
  updating: Record<number, boolean> = {};
  deleting: Record<number, boolean> = {};
  newProduct: Product = {
    productId: 0,
    categoryId: 1,
    productName: '',
    quantityPerUnit: '',
    unitPrice: 0,
    unitsInStock: 0
  };

  constructor(private productService: ProductService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    if (this.loading) return;

    this.loading = true;
    this.listError = null;
    this.productService
      .getAll()
      .pipe(
        timeout(10000),
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res: ApiResponse<Product[]>) => {
          if (!res?.success) {
            this.products = [];
            this.listError = res?.message ?? 'Urunler yuklenemedi.';
            return;
          }

          this.products = extractArray<Product>(res.data);
          this.listError = null;
        },
        error: () => {
          this.listError = 'Urunler yuklenemedi.';
        }
      });
  }

  addProduct(): void {
    if (this.saving) return;
    this.success = null;
    this.formError = null;

    if (!this.newProduct.productName.trim()) {
      this.formError = 'Urun adi gerekli.';
      return;
    }

    this.saving = true;
    this.productService.add({ ...this.newProduct }).subscribe({
      next: (res) => {
        if (res.success) {
          this.success = 'Urun eklendi.';
          this.newProduct.productName = '';
          this.newProduct.quantityPerUnit = '';
          this.newProduct.unitPrice = 0;
          this.newProduct.unitsInStock = 0;
          this.loadProducts();
        } else {
          this.formError = res.message ?? 'Urun eklenemedi.';
        }
        this.saving = false;
      },
      error: () => {
        this.formError = 'Urun eklenemedi.';
        this.saving = false;
      }
    });
  }

  addStock(product: Product): void {
    if (this.updating[product.productId]) return;
    const raw = prompt('Kac adet stok eklemek istiyorsunuz?');
    if (!raw) return;
    const amount = Number(raw);
    if (!Number.isFinite(amount) || amount <= 0) {
      this.formError = 'Gecerli bir stok miktari girin.';
      return;
    }

    this.updating[product.productId] = true;
    const updated: Product = { ...product, unitsInStock: product.unitsInStock + amount };
    this.productService.update(updated).subscribe({
      next: (res) => {
        if (res.success) {
          product.unitsInStock = updated.unitsInStock;
          this.success = 'Stok guncellendi.';
        } else {
          this.formError = res.message ?? 'Stok guncellenemedi.';
        }
        this.updating[product.productId] = false;
      },
      error: () => {
        this.formError = 'Stok guncellenemedi.';
        this.updating[product.productId] = false;
      }
    });
  }

  deleteProduct(product: Product): void {
    if (this.deleting[product.productId]) return;
    const ok = confirm('Silmek istediginize emin misiniz?');
    if (!ok) return;

    this.deleting[product.productId] = true;
    this.productService.delete(product.productId).subscribe({
      next: (res) => {
        if (res.success) {
          this.products = this.products.filter((p) => p.productId !== product.productId);
          this.success = 'Urun silindi.';
        } else {
          this.formError = res.message ?? 'Urun silinemedi.';
        }
        this.deleting[product.productId] = false;
      },
      error: () => {
        this.formError = 'Urun silinemedi.';
        this.deleting[product.productId] = false;
      }
    });
  }
}
