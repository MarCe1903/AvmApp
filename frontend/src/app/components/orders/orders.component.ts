import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { OrderService, MyOrder } from '../../services/order.service';
import { Subject, catchError, finalize, map, of, takeUntil, timeout, timer } from 'rxjs';
import { extractArray } from '../../utils/response-array';

const ORDER_POLLING_INTERVAL_MS = 3000;

@Component({
  selector: 'app-orders',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './orders.component.html',
  styleUrls: ['./orders.component.css']
})
export class OrdersComponent implements OnInit, OnDestroy {
  orders: MyOrder[] = [];
  loading = false;
  error: string | null = null;

  private readonly destroy$ = new Subject<void>();
  private requestInFlight = false;

  constructor(private orderService: OrderService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    timer(0, ORDER_POLLING_INTERVAL_MS)
      .pipe(takeUntil(this.destroy$))
      .subscribe((tick) => this.loadOrders(tick === 0));
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadOrders(showLoader: boolean = true): void {
    if (this.requestInFlight) return;
    this.requestInFlight = true;

    if (showLoader) {
      this.loading = true;
    }

    this.orderService
      .getMyOrders()
      .pipe(
        timeout(10000),
        map((res: unknown) => this.mapOrdersResponse(res)),
        catchError((err) => {
          console.error('[orders] getMyOrders error:', err);
          this.error = 'Siparisler yuklenemedi.';
          // Polling hatasinda mevcut listeyi korumak icin null donuyoruz.
          return of<MyOrder[] | null>(null);
        }),
        finalize(() => {
          this.requestInFlight = false;
          this.loading = false;
          this.cdr.detectChanges();
        }),
        takeUntil(this.destroy$)
      )
      .subscribe((mapped) => {
        if (mapped !== null) {
          this.orders = mapped;
          this.error = null;
          console.log('[orders] mapped count:', mapped.length);
        }
      });
  }

  private mapOrdersResponse(res: unknown): MyOrder[] {
    return extractArray<MyOrder>(res);
  }
}
