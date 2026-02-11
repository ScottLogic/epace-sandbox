import { Injectable, OnDestroy } from '@angular/core';
import { Observable, Subscription, catchError, throwError } from 'rxjs';
import { RpcClient, ConnectionState } from '../rpc';
import { Symbol, TradeUpdate } from './models/trade-update';

@Injectable({ providedIn: 'root' })
export class BlockchainRpcService implements OnDestroy {
  private subscription: Subscription | null = null;

  constructor(private readonly rpcClient: RpcClient) {}

  get connectionState$(): Observable<ConnectionState> {
    return this.rpcClient.connectionState$;
  }

  connect(): Promise<void> {
    return this.rpcClient.connect();
  }

  disconnect(): Promise<void> {
    return this.rpcClient.disconnect();
  }

  subscribe(symbol: Symbol): Observable<unknown> {
    return this.rpcClient.invoke('subscribe', { channel: 'trades', symbol }).pipe(
      catchError((err: unknown) => {
        const message =
          err instanceof Error ? err.message : 'Failed to subscribe to trade updates';
        return throwError(() => new Error(message));
      }),
    );
  }

  unsubscribe(symbol: Symbol): Observable<unknown> {
    return this.rpcClient.invoke('unsubscribe', { channel: 'trades', symbol }).pipe(
      catchError((err: unknown) => {
        const message =
          err instanceof Error ? err.message : 'Failed to unsubscribe from trade updates';
        return throwError(() => new Error(message));
      }),
    );
  }

  onTradeUpdate(): Observable<TradeUpdate> {
    return this.rpcClient.onNotification<TradeUpdate>('trades.update').pipe(
      catchError((err: unknown) => {
        const message =
          err instanceof Error ? err.message : 'Error receiving trade updates';
        return throwError(() => new Error(message));
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscription?.unsubscribe();
    this.subscription = null;
    this.rpcClient.dispose();
  }
}
