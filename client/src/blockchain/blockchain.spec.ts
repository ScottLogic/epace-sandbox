import { TestBed } from '@angular/core/testing';
import { RpcClient } from '../rpc';
import { MockRpcConnection } from '../rpc/testing/mock-rpc-connection';
import { BlockchainMethods } from './models/blockchain-methods';
import { Blockchain } from './blockchain';

describe('Blockchain', () => {
  let mockConnection: MockRpcConnection;

  beforeEach(async () => {
    mockConnection = new MockRpcConnection();

    await TestBed.configureTestingModule({
      imports: [Blockchain],
      providers: [
        {
          provide: RpcClient,
          useFactory: () => new RpcClient<BlockchainMethods>(mockConnection, { timeout: 1000 }),
        },
      ],
    }).compileComponents();
  });

  afterEach(() => {
    mockConnection.dispose();
  });

  it('should create the component', () => {
    const fixture = TestBed.createComponent(Blockchain);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
