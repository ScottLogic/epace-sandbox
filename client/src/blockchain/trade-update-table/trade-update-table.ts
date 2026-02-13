import { Component, input } from '@angular/core';
import { DatePipe, UpperCasePipe } from '@angular/common';
import { TradeUpdate } from '../models/trade-update';

@Component({
  selector: 'app-trade-update-table',
  imports: [DatePipe, UpperCasePipe],
  templateUrl: './trade-update-table.html',
  styleUrl: './trade-update-table.css',
})
export class TradeUpdateTable {
  trades = input<TradeUpdate[]>([]);
}
