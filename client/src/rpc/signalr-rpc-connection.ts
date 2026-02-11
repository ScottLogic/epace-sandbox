import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { ConnectionState } from './models';
import { RpcConnection } from './rpc-connection';

export interface SignalRConnectionOptions {
  sendMethodName?: string;
  receiveMethodName?: string;
  debug?: boolean;
  logger?: (message?: unknown, ...optionalParams: unknown[]) => void;
}

const DEFAULT_SEND_METHOD = 'SendMessage';
const DEFAULT_RECEIVE_METHOD = 'ReceiveMessage';

export class SignalRRpcConnection implements RpcConnection {
  private readonly hubConnection: HubConnection;
  private readonly stateSubject = new BehaviorSubject<ConnectionState>('disconnected');
  private readonly messagesSubject = new Subject<string>();
  private readonly sendMethod: string;
  private readonly debug: boolean;
  private readonly log: (message?: unknown, ...optionalParams: unknown[]) => void;

  constructor(hubConnection: HubConnection, options?: SignalRConnectionOptions) {
    this.hubConnection = hubConnection;
    this.sendMethod = options?.sendMethodName ?? DEFAULT_SEND_METHOD;
    const receiveMethod = options?.receiveMethodName ?? DEFAULT_RECEIVE_METHOD;
    this.debug = !!options?.debug;
    this.log = options?.logger ?? console.debug.bind(console);

    this.hubConnection.on(receiveMethod, (message: string) => {
      if (this.debug) this.log('[SignalR] message received', { method: receiveMethod, message });
      this.messagesSubject.next(message);
    });

    this.hubConnection.onclose((error) => {
      if (this.debug) this.log('[SignalR] connection closed', { error });
      this.stateSubject.next('disconnected');
    });
    this.hubConnection.onreconnecting((error) => {
      if (this.debug) this.log('[SignalR] reconnecting', { error });
      this.stateSubject.next('reconnecting');
    });
    this.hubConnection.onreconnected((connectionId) => {
      if (this.debug) this.log('[SignalR] reconnected', { connectionId });
      this.stateSubject.next('connected');
    });
  }

  static create(hubUrl: string, options?: SignalRConnectionOptions): SignalRRpcConnection {
    const hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect()
      .build();
    return new SignalRRpcConnection(hubConnection, options);
  }

  get state$(): Observable<ConnectionState> {
    return this.stateSubject.asObservable();
  }

  get messages$(): Observable<string> {
    return this.messagesSubject.asObservable();
  }

  async connect(): Promise<void> {
    this.stateSubject.next('connecting');
    if (this.debug) this.log('[SignalR] starting connection');
    await this.hubConnection.start().catch((err) => {
      if (this.debug) this.log('[SignalR] start failed', err);
      throw err;
    });
    if (this.debug) this.log('[SignalR] connected');
    this.stateSubject.next('connected');
  }

  async disconnect(): Promise<void> {
    if (this.debug) this.log('[SignalR] stopping connection');
    await this.hubConnection.stop().catch((err) => {
      if (this.debug) this.log('[SignalR] stop failed', err);
      throw err;
    });
    if (this.debug) this.log('[SignalR] disconnected');
    this.stateSubject.next('disconnected');
  }

  async send(data: string): Promise<void> {
    if (this.debug) this.log('[SignalR] send', { method: this.sendMethod, data });
    await this.hubConnection.invoke(this.sendMethod, data).catch((err) => {
      if (this.debug) this.log('[SignalR] send failed', err);
      throw err;
    });
  }
}
