import { TestBed } from '@angular/core/testing';
import { Blockchain } from './blockchain';
import { BlockchainRpcService } from './blockchain-rpc.service';
import { TradeUpdate } from './models/trade-update';
import { MockRpcConnection } from '../rpc/testing/mock-rpc-connection';
import { RpcClient } from '../rpc';
import { BlockchainMethods } from './models/blockchain-methods';
import { RPC_CONNECTION } from '../rpc/rpc-client.service';

function tick(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

function createTrade(symbol: 'BTC-USD' | 'ETH-USD', tradeId: string): TradeUpdate {
  return {
    seqnum: 1,
    event: 'updated',
    channel: 'trades',
    symbol,
    timestamp: '2026-01-01T00:00:00Z',
    side: 'buy',
    qty: 0.5,
    price: 50000,
    tradeId,
  };
}

describe('Blockchain', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Blockchain],
    }).compileComponents();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(Blockchain);
    expect(fixture.componentInstance).toBeTruthy();
  });
});

describe('Blockchain loading state', () => {
  let connection: MockRpcConnection;
  let component: Blockchain;

  beforeEach(async () => {
    connection = new MockRpcConnection();

    await TestBed.configureTestingModule({
      imports: [Blockchain],
      providers: [
        { provide: RPC_CONNECTION, useValue: connection },
        {
          provide: RpcClient,
          useFactory: () =>
            new RpcClient<BlockchainMethods>(connection, { timeout: 1000 }),
        },
        BlockchainRpcService,
      ],
    }).compileComponents();

    const fixture = TestBed.createComponent(Blockchain);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await tick();
  });

  afterEach(() => {
    component.ngOnDestroy();
    connection.dispose();
  });

  it('should keep loading true after subscription succeeds until first trade arrives', async () => {
    component.onSymbolSelected('BTC-USD');
    await tick();

    const sent = JSON.parse(connection.sentMessages[0]);
    connection.simulateMessage(
      JSON.stringify({ jsonrpc: '2.0', result: { event: 'subscribed' }, id: sent.id }),
    );
    await tick();

    const sub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    expect(sub).toBeDefined();
    expect(sub!.loading).toBe(true);
    expect(sub!.trades).toHaveLength(0);
  });

  it('should set loading to false when the first trade arrives', async () => {
    component.onSymbolSelected('BTC-USD');
    await tick();

    const sent = JSON.parse(connection.sentMessages[0]);
    connection.simulateMessage(
      JSON.stringify({ jsonrpc: '2.0', result: { event: 'subscribed' }, id: sent.id }),
    );
    await tick();

    connection.simulateMessage(
      JSON.stringify({
        jsonrpc: '2.0',
        method: 'trades.update',
        params: createTrade('BTC-USD', 'trade-1'),
      }),
    );
    await tick();

    const sub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    expect(sub).toBeDefined();
    expect(sub!.loading).toBe(false);
    expect(sub!.trades).toHaveLength(1);
  });

  it('should not affect loading of other symbols when a trade arrives', async () => {
    component.onSymbolSelected('BTC-USD');
    await tick();
    const sent1 = JSON.parse(connection.sentMessages[0]);
    connection.simulateMessage(
      JSON.stringify({ jsonrpc: '2.0', result: { event: 'subscribed' }, id: sent1.id }),
    );
    await tick();

    component.onSymbolSelected('ETH-USD');
    await tick();
    const sent2 = JSON.parse(connection.sentMessages[1]);
    connection.simulateMessage(
      JSON.stringify({ jsonrpc: '2.0', result: { event: 'subscribed' }, id: sent2.id }),
    );
    await tick();

    connection.simulateMessage(
      JSON.stringify({
        jsonrpc: '2.0',
        method: 'trades.update',
        params: createTrade('BTC-USD', 'trade-1'),
      }),
    );
    await tick();

    const btcSub = component.subscriptions.find((s) => s.symbol === 'BTC-USD');
    const ethSub = component.subscriptions.find((s) => s.symbol === 'ETH-USD');
    expect(btcSub!.loading).toBe(false);
    expect(ethSub!.loading).toBe(true);
  });
});
