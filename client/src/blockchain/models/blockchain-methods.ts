import { RpcMethodDefinition } from '../../rpc';
import { Channel, Symbol, TradeUpdate } from './trade-update';

export interface SubscribeParams {
  channel: Channel;
  symbol: Symbol;
}

export interface SubscribeResult {
  event: string;
}

export interface GetRecentTradesParams {
  channel: Channel;
  symbol: Symbol;
  count: number;
  beforeTimestamp?: string;
}

export interface BlockchainMethods {
  subscribe: RpcMethodDefinition<SubscribeParams, SubscribeResult>;
  unsubscribe: RpcMethodDefinition<SubscribeParams, SubscribeResult>;
  getRecentTrades: RpcMethodDefinition<GetRecentTradesParams, TradeUpdate[]>;
}
