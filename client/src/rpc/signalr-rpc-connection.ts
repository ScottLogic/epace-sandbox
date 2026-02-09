import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { BehaviorSubject, Observable, Subject } from 'rxjs';
import { ConnectionState } from './models';
import { RpcConnection } from './rpc-connection';

export interface SignalRConnectionOptions {
  sendMethodName?: string;
  receiveMethodName?: string;
}

const DEFAULT_SEND_METHOD = 'SendMessage';
const DEFAULT_RECEIVE_METHOD = 'ReceiveMessage';

export class SignalRRpcConnection implements RpcConnection {
  private readonly hubConnection: HubConnection;
  private readonly stateSubject = new BehaviorSubject<ConnectionState>('disconnected');
  private readonly messagesSubject = new Subject<string>();
  private readonly sendMethod: string;

  constructor(hubConnection: HubConnection, options?: SignalRConnectionOptions) {
    this.hubConnection = hubConnection;
    this.sendMethod = options?.sendMethodName ?? DEFAULT_SEND_METHOD;
    const receiveMethod = options?.receiveMethodName ?? DEFAULT_RECEIVE_METHOD;

    this.hubConnection.on(receiveMethod, (message: string) => {
      this.messagesSubject.next(message);
    });

    this.hubConnection.onclose(() => this.stateSubject.next('disconnected'));
    this.hubConnection.onreconnecting(() => this.stateSubject.next('reconnecting'));
    this.hubConnection.onreconnected(() => this.stateSubject.next('connected'));
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
    await this.hubConnection.start();
    this.stateSubject.next('connected');
  }

  async disconnect(): Promise<void> {
    await this.hubConnection.stop();
    this.stateSubject.next('disconnected');
  }

  async send(data: string): Promise<void> {
    await this.hubConnection.invoke(this.sendMethod, data);
  }
}
