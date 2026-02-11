import { InjectionToken, Provider, isDevMode } from '@angular/core';
import { RpcClient, RpcClientOptions } from './rpc-client';
import { SignalRConnectionOptions, SignalRRpcConnection } from './signalr-rpc-connection';
import { RpcMethodDefinition, RpcMethodMap } from './models';
import { RpcConnection } from './rpc-connection';

export interface RpcClientConfig {
  hubUrl: string;
  connectionOptions?: SignalRConnectionOptions;
  clientOptions?: RpcClientOptions;
}

export const RPC_CONNECTION = new InjectionToken<RpcConnection>('RPC_CONNECTION');

export function provideRpcClient<TMethods extends { [K in keyof TMethods]: RpcMethodDefinition } = RpcMethodMap>(
  config: RpcClientConfig,
): Provider[] {
  const debug = config.clientOptions?.debug ?? isDevMode();
  const connectionOpts: SignalRConnectionOptions = { ...config.connectionOptions, debug };
  const clientOpts: RpcClientOptions = { ...config.clientOptions, debug };

  return [
    {
      provide: RPC_CONNECTION,
      useFactory: () =>
        SignalRRpcConnection.create(config.hubUrl, connectionOpts),
    },
    {
      provide: RpcClient,
      useFactory: (connection: RpcConnection) =>
        new RpcClient<TMethods>(connection, clientOpts),
      deps: [RPC_CONNECTION],
    },
  ];
}

export function createRpcClient<TMethods extends { [K in keyof TMethods]: RpcMethodDefinition } = RpcMethodMap>(
  connection: RpcConnection,
  options?: RpcClientOptions,
): RpcClient<TMethods> {
  return new RpcClient<TMethods>(connection, options);
}
