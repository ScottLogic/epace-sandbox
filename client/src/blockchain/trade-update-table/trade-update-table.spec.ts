import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TradeUpdateTable } from './trade-update-table';
import { TradeUpdate } from '../models/trade-update';

function createTrade(overrides?: Partial<TradeUpdate>): TradeUpdate {
  return {
    seqnum: 1,
    event: 'updated',
    channel: 'trades',
    symbol: 'BTC-USD',
    timestamp: '2026-01-01T00:00:00Z',
    side: 'buy',
    qty: 0.5,
    price: 50000,
    tradeId: `trade-${Math.random()}`,
    ...overrides,
  };
}

describe('TradeUpdateTable', () => {
  let fixture: ComponentFixture<TradeUpdateTable>;
  let component: TradeUpdateTable;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TradeUpdateTable],
    }).compileComponents();

    fixture = TestBed.createComponent(TradeUpdateTable);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should show no-trades message when trades array is empty', () => {
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.no-trades')).toBeTruthy();
    expect(el.querySelector('.trade-table')).toBeFalsy();
  });

  it('should render a table when trades are provided', () => {
    fixture.componentRef.setInput('trades', [createTrade()]);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelector('.trade-table')).toBeTruthy();
    expect(el.querySelectorAll('tbody tr')).toHaveLength(1);
  });

  it('should render correct number of rows', () => {
    fixture.componentRef.setInput('trades', [createTrade(), createTrade(), createTrade()]);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelectorAll('tbody tr')).toHaveLength(3);
  });

  it('should apply side-buy class for buy trades', () => {
    fixture.componentRef.setInput('trades', [createTrade({ side: 'buy' })]);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    const row = el.querySelector('tbody tr');
    expect(row?.classList.contains('side-buy')).toBe(true);
  });

  it('should apply side-sell class for sell trades', () => {
    fixture.componentRef.setInput('trades', [createTrade({ side: 'sell' })]);
    fixture.detectChanges();
    const el: HTMLElement = fixture.nativeElement;
    const row = el.querySelector('tbody tr');
    expect(row?.classList.contains('side-sell')).toBe(true);
  });
});
