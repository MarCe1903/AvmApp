import { ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';
import { ApiResponse } from '../../models/api-response';
import { ProductDetail } from '../../models/product-detail';
import { AuthResponse, AuthService } from '../../services/auth.service';
import { ChatService, ChatSessionListItem } from '../../services/chat.service';
import { FavoriteService } from '../../services/favorite.service';
import { OrderService } from '../../services/order.service';
import { ProductService } from '../../services/product.service';
import { extractArray } from '../../utils/response-array';

interface CartItem {
  product: ProductDetail;
  quantity: number;
}

interface ChatMessageView {
  senderRole: 'User' | 'Seller';
  text: string;
  sentAt: Date;
}

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './product-list.html',
  styleUrls: ['./product-list.css']
})
export class ProductListComponent implements OnInit, OnDestroy {
  products: ProductDetail[] = [];
  filteredProducts: ProductDetail[] = [];
  loading = false;
  error: string | null = null;
  searchTerm = '';
  selectedCategory = 'all';
  lastUpdated: Date | null = null;

  showCartPanel = false;
  expandedProductId: number | null = null;
  cartItems: CartItem[] = [];

  favoriteMessage: string | null = null;
  favoriteIds = new Set<number>();
  favoritesLoading = false;
  favoritesError: string | null = null;

  chatMessages: ChatMessageView[] = [];
  chatSessions: ChatSessionListItem[] = [];
  activeChatSessionId: number | null = null;
  newChatMessage = '';
  showChatPanel = false;
  chatLoading = false;
  chatSending = false;
  chatError: string | null = null;

  isAuthenticated = false;
  isAdminUser = false;
  isSellerUser = false;
  currentUserName: string | null = null;
  currentUserRole: string | null = null;

  private favoriteMessageTimer: ReturnType<typeof setTimeout> | null = null;
  private sessionSub?: Subscription;

  constructor(
    private productService: ProductService,
    private authService: AuthService,
    private favoriteService: FavoriteService,
    private chatService: ChatService,
    private orderService: OrderService,
    private cdr: ChangeDetectorRef,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.applySession(this.authService.getSession());

    this.sessionSub = this.authService.sessionChanges().subscribe((session) => {
      Promise.resolve().then(() => {
        this.applySession(session);
        this.cdr.detectChanges();
      });
    });
  }

  ngOnDestroy(): void {
    this.favoriteMessageTimer && clearTimeout(this.favoriteMessageTimer);
    this.sessionSub?.unsubscribe();
  }

  loadProducts(): void {
    this.loading = true;
    this.error = null;
    this.filteredProducts = [];

    this.productService
      .getDetails()
      .pipe(
        finalize(() => {
          this.loading = false;
          this.cdr.detectChanges();
        })
      )
      .subscribe({
        next: (res: ApiResponse<ProductDetail[]>) => {
          if (!res.success) {
            this.products = [];
            this.error = res.message ?? 'Urunler yuklenirken bir hata olustu.';
            return;
          }

          this.products = extractArray<ProductDetail>(res.data);
          this.applyFilters();
          this.lastUpdated = new Date();
        },
        error: () => {
          this.products = [];
          this.error = 'Urunler yuklenirken bir hata olustu.';
        }
      });
  }

  get totalStock(): number {
    return this.filteredProducts.reduce((sum, product) => sum + product.unitsInStock, 0);
  }

  get averagePrice(): number {
    if (!this.filteredProducts.length) {
      return 0;
    }

    const total = this.filteredProducts.reduce((sum, product) => sum + product.unitPrice, 0);
    return total / this.filteredProducts.length;
  }

  get categories(): string[] {
    return this.products
      .map((product) => product.categoryName)
      .filter((category, index, list) => list.indexOf(category) === index)
      .sort((a, b) => a.localeCompare(b));
  }

  get cartCount(): number {
    return this.cartItems.reduce((sum, item) => sum + item.quantity, 0);
  }

  get userInitials(): string {
    if (!this.currentUserName) {
      return '';
    }

    return this.currentUserName
      .split(' ')
      .filter(Boolean)
      .map((part) => part[0])
      .join('')
      .substring(0, 2)
      .toUpperCase();
  }

  get favoriteProducts(): ProductDetail[] {
    if (!this.products.length || !this.favoriteIds.size) {
      return [];
    }

    return this.products.filter((product) => this.favoriteIds.has(product.productId));
  }

  refresh(): void {
    this.loadProducts();
  }

  onSearch(value: string): void {
    this.searchTerm = value;
    this.applyFilters();
  }

  onCategoryChange(value: string): void {
    this.selectedCategory = value;
    this.applyFilters();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.selectedCategory = 'all';
    this.applyFilters();
  }

  canClearFilters(): boolean {
    return this.searchTerm.trim().length > 0 || this.selectedCategory !== 'all';
  }

  trackByProductId(index: number, product: ProductDetail): number {
    return product.productId;
  }

  getStockBarWidth(unitsInStock: number): number {
    return Math.min(100, unitsInStock);
  }

  toggleProductDetails(productId: number): void {
    this.expandedProductId = this.expandedProductId === productId ? null : productId;
  }

  addToCart(product: ProductDetail, event?: Event): void {
    event?.stopPropagation();

    if (product.unitsInStock <= 0) {
      this.setFavoriteMessage('Bu urun stokta kalmadi.');
      return;
    }

    const existing = this.cartItems.find((item) => item.product.productId === product.productId);
    if (existing) {
      existing.quantity += 1;
    } else {
      this.cartItems.push({ product, quantity: 1 });
    }

    this.showCartPanel = true;
  }

  purchaseProduct(product: ProductDetail, event?: Event): void {
    event?.stopPropagation();

    if (!this.ensureAuthenticated('Satin almak icin once giris yapmalisiniz.')) {
      return;
    }

    if (product.unitsInStock <= 0) {
      this.setFavoriteMessage('Stokta urun kalmadi.');
      return;
    }

    this.orderService.purchase(product.productId).subscribe({
      next: () => {
        product.unitsInStock -= 1;
        this.setFavoriteMessage('Satin alma basarili. Siparislerimden takip edebilirsiniz.');
      },
      error: (err) => {
        this.setFavoriteMessage(err?.error?.message ?? 'Satin alma basarisiz.');
      }
    });
  }

  removeFromCart(productId: number, event?: Event): void {
    event?.stopPropagation();
    this.cartItems = this.cartItems.filter((item) => item.product.productId !== productId);
  }

  toggleCartPanel(): void {
    this.showCartPanel = !this.showCartPanel;
  }

  isOutOfStock(product: ProductDetail): boolean {
    return product.unitsInStock <= 0;
  }

  toggleChatPanel(): void {
    if (!this.ensureAuthenticated('Sohbet icin once giris yapmalisiniz.')) {
      return;
    }

    this.showChatPanel = !this.showChatPanel;
    if (this.showChatPanel) {
      this.refreshChat();
    }
  }

  redirectToAuth(tab: 'login' | 'register' = 'login'): void {
    this.router.navigate(['/auth'], {
      queryParams: {
        tab,
        returnUrl: this.router.url
      }
    });
  }

  logout(): void {
    this.authService.clearSession();
    this.clearSessionState();
  }

  refreshChat(): void {
    if (this.isSellerUser) {
      this.loadChatSessions();
      return;
    }

    this.ensureChatSession();
  }

  loadChatSessions(): void {
    if (!this.isSellerUser) {
      return;
    }

    this.chatLoading = true;
    this.chatError = null;

    this.chatService.getSessions().subscribe({
      next: (sessions) => {
        this.chatSessions = extractArray<ChatSessionListItem>(sessions);
        if (!this.activeChatSessionId && this.chatSessions.length) {
          this.activeChatSessionId = this.chatSessions[0].id;
          this.loadChatMessages(this.activeChatSessionId);
        }
        this.chatLoading = false;
      },
      error: () => {
        this.chatError = 'Sohbetler yuklenemedi.';
        this.chatLoading = false;
      }
    });
  }

  selectChatSession(sessionId: number): void {
    if (this.activeChatSessionId === sessionId) {
      return;
    }

    this.activeChatSessionId = sessionId;
    this.loadChatMessages(sessionId);
  }

  ensureChatSession(): void {
    this.chatLoading = true;
    this.chatError = null;

    this.chatService.startChat().subscribe({
      next: (session) => {
        this.activeChatSessionId = session.id;
        this.loadChatMessages(session.id);
        this.chatLoading = false;
      },
      error: () => {
        this.chatError = 'Sohbet baslatilamadi.';
        this.chatLoading = false;
      }
    });
  }

  loadChatMessages(sessionId: number): void {
    this.chatService.getMessages(sessionId).subscribe({
      next: (messages) => {
        this.chatMessages = extractArray<{
          senderRole: string;
          text: string;
          sentAt: string;
        }>(messages).map((message) => ({
          senderRole: message.senderRole as 'User' | 'Seller',
          text: message.text,
          sentAt: new Date(message.sentAt)
        }));
        this.cdr.detectChanges();
      },
      error: () => {
        this.chatError = 'Mesajlar yuklenemedi.';
      }
    });
  }

  sendChatMessage(): void {
    if (!this.newChatMessage.trim() || this.chatSending || !this.activeChatSessionId) {
      return;
    }

    this.chatSending = true;
    const payload = this.newChatMessage.trim();
    const sessionId = this.activeChatSessionId;

    this.chatService.sendMessage(sessionId, payload).subscribe({
      next: () => {
        this.newChatMessage = '';
        this.loadChatMessages(sessionId);
        this.chatSending = false;
      },
      error: () => {
        this.chatError = 'Mesaj gonderilemedi.';
        this.chatSending = false;
      }
    });
  }

  closeChat(): void {
    if (!this.activeChatSessionId) {
      return;
    }

    this.chatService.closeChat(this.activeChatSessionId).subscribe({
      next: () => {
        this.chatMessages = [];

        if (this.isSellerUser) {
          this.chatSessions = this.chatSessions.filter((session) => session.id !== this.activeChatSessionId);
          this.activeChatSessionId = this.chatSessions[0]?.id ?? null;

          if (this.activeChatSessionId) {
            this.loadChatMessages(this.activeChatSessionId);
          }

          return;
        }

        this.activeChatSessionId = null;
      },
      error: () => {
        this.chatError = 'Sohbet kapatilamadi.';
      }
    });
  }

  requireLoginForFavorites(product: ProductDetail, event?: Event): void {
    if (this.isAuthenticated) {
      return;
    }

    event?.stopPropagation();
    this.setFavoriteMessage(`"${product.productName}" icin once hesabiniza giris yapmaniz gerekiyor.`);
    this.redirectToAuth('login');
  }

  loadFavorites(): void {
    if (!this.isAuthenticated) {
      return;
    }

    this.favoritesLoading = true;
    this.favoritesError = null;

    this.favoriteService.getFavorites().subscribe({
      next: (ids) => {
        this.favoriteIds = new Set(extractArray<number>(ids));
        this.favoritesLoading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.favoritesError = 'Favoriler yuklenemedi.';
        this.favoritesLoading = false;
      }
    });
  }

  toggleFavorite(product: ProductDetail, event?: Event): void {
    event?.stopPropagation();

    if (!this.isAuthenticated) {
      this.requireLoginForFavorites(product, event);
      return;
    }

    if (this.isFavorited(product)) {
      this.favoriteService.removeFavorite(product.productId).subscribe({
        next: () => {
          this.favoriteIds.delete(product.productId);
          this.cdr.detectChanges();
        },
        error: () => {
          this.setFavoriteMessage('Favori kaldirilamadi.');
        }
      });
      return;
    }

    this.favoriteService.addFavorite(product.productId).subscribe({
      next: () => {
        this.favoriteIds.add(product.productId);
        this.cdr.detectChanges();
      },
      error: () => {
        this.setFavoriteMessage('Favoriye eklenemedi.');
      }
    });
  }

  isFavorited(product: ProductDetail): boolean {
    return this.favoriteIds.has(product.productId);
  }

  private applyFilters(): void {
    const term = this.searchTerm.trim().toLowerCase();

    this.filteredProducts = this.products.filter((product) => {
      const matchesCategory = this.selectedCategory === 'all' || product.categoryName === this.selectedCategory;
      const matchesTerm =
        !term ||
        product.productName.toLowerCase().includes(term) ||
        product.categoryName.toLowerCase().includes(term);

      return matchesCategory && matchesTerm;
    });
  }

  private applySession(session: AuthResponse | null): void {
    if (!session) {
      this.clearSessionState();
      return;
    }

    this.isAuthenticated = true;
    this.isAdminUser = session.role === 'Admin';
    this.isSellerUser = session.role === 'Seller';
    this.currentUserName = session.fullName;
    this.currentUserRole = session.role;
    this.loadFavorites();
  }

  private clearSessionState(): void {
    this.isAuthenticated = false;
    this.isAdminUser = false;
    this.isSellerUser = false;
    this.currentUserName = null;
    this.currentUserRole = null;
    this.favoriteIds.clear();
    this.favoritesError = null;
    this.favoritesLoading = false;
    this.chatSessions = [];
    this.chatMessages = [];
    this.activeChatSessionId = null;
    this.chatError = null;
    this.chatLoading = false;
  }

  private ensureAuthenticated(message: string): boolean {
    if (this.isAuthenticated) {
      return true;
    }

    this.setFavoriteMessage(message);
    this.redirectToAuth('login');
    return false;
  }

  private setFavoriteMessage(message: string): void {
    this.favoriteMessage = message;

    if (this.favoriteMessageTimer) {
      clearTimeout(this.favoriteMessageTimer);
    }

    this.favoriteMessageTimer = setTimeout(() => {
      this.favoriteMessage = null;
    }, 3000);
  }
}
