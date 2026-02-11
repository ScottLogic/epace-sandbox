import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { Subscription } from 'rxjs';
import { BlockchainRpcService } from './blockchain-rpc.service';
import { SymbolSelector } from './symbol-selector/symbol-selector';
import { SubscriptionContainer } from './subscription-container/subscription-container';
import { TradeUpdate, Symbol } from './models/trade-update';
import { ConnectionState } from '../rpc';

interface SymbolSubscription {
  symbol: string;
  trades: TradeUpdate[];
  loading: boolean;
}

@Component({
  selector: 'app-blockchain',
  imports: [SymbolSelector, SubscriptionContainer],
  templateUrl: './blockchain.html',
  styleUrl: './blockchain.css',
})

export class Blockchain implements OnInit, OnDestroy {
  subscriptions: SymbolSubscription[] = [];
  connectionError = '';
  connectionState: ConnectionState = 'disconnected';

  private tradeSubscription: Subscription | null = null;
  private stateSubscription: Subscription | null = null;

  constructor(
    private readonly rpcService: BlockchainRpcService,
    private readonly cdr: ChangeDetectorRef,
  ) {}

  get activeSymbols(): string[] {
    return this.subscriptions.map((s) => s.symbol);
  }

  ngOnInit(): void {
    this.stateSubscription = this.rpcService.connectionState$.subscribe((state) => {
      this.connectionState = state;
      if (state === 'connected') {
        this.connectionError = '';
      }
      this.cdr.detectChanges();
    });

    this.rpcService
      .connect()
      .then(() => {
        this.listenForTrades();
      })
      .catch((err: unknown) => {
        this.connectionError =
          err instanceof Error ? err.message : 'Failed to connect to server';
        this.cdr.detectChanges();
      });
  }

  ngOnDestroy(): void {
    this.tradeSubscription?.unsubscribe();
    this.stateSubscription?.unsubscribe();

    for (const sub of this.subscriptions) {
      this.rpcService.unsubscribe(sub.symbol as Symbol).subscribe();
    }

    this.rpcService.disconnect().catch(() => {});
  }

  onSymbolSelected(symbol: string): void {
    if (this.subscriptions.some((s) => s.symbol === symbol)) {
      return;
    }

    const entry: SymbolSubscription = { symbol, trades: [], loading: true };
    this.subscriptions = [...this.subscriptions, entry];

    this.rpcService.subscribe(symbol as Symbol).subscribe({
      next: () => {
        this.updateSubscription(symbol, { loading: false });
        this.cdr.detectChanges();
      },
      error: (err: unknown) => {
        this.subscriptions = this.subscriptions.filter((s) => s.symbol !== symbol);
        this.connectionError =
          err instanceof Error ? err.message : `Failed to subscribe to ${symbol}`;
        this.cdr.detectChanges();
      },
    });
  }

  onUnsubscribe(symbol: string): void {
    this.subscriptions = this.subscriptions.filter((s) => s.symbol !== symbol);
    this.rpcService.unsubscribe(symbol as Symbol).subscribe();
  }

  dismissError(): void {
    this.connectionError = '';
  }

  private listenForTrades(): void {
    this.tradeSubscription = this.rpcService.onTradeUpdate().subscribe((trade) => {
      this.subscriptions = this.subscriptions.map((s) =>
        s.symbol === trade.symbol
          ? { ...s, trades: [trade, ...s.trades], loading: false }
          : s,
      );
      this.cdr.detectChanges();
    });
  }

  private updateSubscription(
    symbol: string,
    updates: Partial<Omit<SymbolSubscription, 'symbol'>>,
  ): void {
    this.subscriptions = this.subscriptions.map((s) =>
      s.symbol === symbol ? { ...s, ...updates } : s,
    );
  }
}
