import { Component, input, output } from '@angular/core';
import { TradeUpdate } from '../models/trade-update';
import { TradeUpdateList } from '../trade-update-list/trade-update-list';
import { TradeUpdateTable } from '../trade-update-table/trade-update-table';

export type ViewMode = 'card' | 'table';

@Component({
  selector: 'app-subscription-container',
  imports: [TradeUpdateList, TradeUpdateTable],
  templateUrl: './subscription-container.html',
  styleUrl: './subscription-container.css',
})
export class SubscriptionContainer {
  symbol = input.required<string>();
  trades = input<TradeUpdate[]>([]);
  loading = input<boolean>(false);
  state = input<'active' | 'paused'>('active');
  unsubscribed = output<string>();
  resubscribed = output<string>();
  dismissed = output<string>();
  viewModeChanged = output<ViewMode>();
  viewMode: ViewMode = 'card';

  toggleViewMode(): void {
    this.viewMode = this.viewMode === 'card' ? 'table' : 'card';
    this.viewModeChanged.emit(this.viewMode);
  }

  onUnsubscribe(): void {
    this.unsubscribed.emit(this.symbol());
  }

  onResubscribe(): void {
    this.resubscribed.emit(this.symbol());
  }

  onDismiss(): void {
    this.dismissed.emit(this.symbol());
  }
}
