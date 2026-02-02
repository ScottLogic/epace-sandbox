# DataServer Architecture

This document describes the architecture for the DataServer backend, which provides real-time market data from external exchanges (starting with Blockchain.com Exchange).

## Overview

The backend follows Clean Architecture principles with clear separation of concerns across layers. Data flows from external WebSocket APIs through the application and out to consumers (REST API, SignalR).

## Project Structure

```
server/
├── DataServer.Api/              # HTTP API and SignalR endpoints
├── DataServer.Application/      # Business logic, interfaces, services
├── DataServer.Connectors/       # External data source implementations
├── DataServer.Domain/           # Core entities and enums
├── DataServer.Infrastructure/   # Caching, logging, cross-cutting concerns
└── DataServer.Tests/            # Unit and integration tests
```

## Layer Dependencies

```
                    ┌─────────────────┐
                    │  DataServer.Api │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
┌─────────────────┐ ┌───────────────┐ ┌──────────────────────┐
│ DataServer.     │ │ DataServer.   │ │ DataServer.          │
│ Connectors      │ │ Infrastructure│ │ Application          │
└────────┬────────┘ └───────┬───────┘ └──────────┬───────────┘
         │                  │                    │
         └──────────────────┼────────────────────┘
                            │
                            ▼
                  ┌──────────────────┐
                  │ DataServer.Domain│
                  └──────────────────┘
```

- **Domain** has no dependencies (core entities)
- **Application** depends on Domain (defines interfaces, services)
- **Connectors** depends on Domain (implements data source interfaces)
- **Infrastructure** depends on Domain and Application (implements repository interfaces)
- **Api** depends on Application, Connectors, Infrastructure (wires everything together)

## Data Flow

```
External WebSocket ──► IBlockchainDataSource ──► BlockchainDataService ──► IBlockchainDataRepository
        │                    (Connectors)           (Application)              (Infrastructure)
        │                         │                      │                           │
        │                         │                      │                           │
        │                    TradeReceived          TradeReceived                    │
        │                      event                  event                          │
        │                         │                      │                           │
        │                         └──────────────────────┼───────────────────────────┘
        │                                                │
        │                                                ▼
        │                                          API / SignalR
        │                                          (real-time push)
        │
        └──────────────────────────────────────────────────────────────────────────────►
                                                                                  REST API
                                                                              (query cached data)
```

## Domain Layer

Contains core entities for the Blockchain.com Exchange WebSocket API.

### Enums

| Enum | Purpose |
|------|---------|
| `Channel` | WebSocket channels (heartbeat, l2, l3, prices, symbols, ticker, trades, auth, balances, trading) |
| `Event` | Response event types (subscribed, unsubscribed, rejected, snapshot, updated) |
| `SubscriptionAction` | Request actions (subscribe, unsubscribe) |
| `Symbol` | Trading pairs (ETH-USD, BTC-USD) |
| `Side` | Trade side (buy, sell) |

All enums have `[EnumMember]` attributes for JSON serialization mapping.

### Records

| Record | Purpose |
|--------|---------|
| `BaseRequest` | Base WebSocket request (action, channel) |
| `TradeRequest` | Trade subscription request (extends BaseRequest with symbol) |
| `BaseResponse` | Base WebSocket response (seqnum, event, channel) |
| `TradeResponse` | Trade subscription response (extends BaseResponse with symbol) |
| `TradeUpdate` | Trade execution data (seqnum, event, channel, symbol, timestamp, side, qty, price, trade_id) |

## Application Layer

Defines interfaces and orchestrates data flow.

### Interfaces

**IBlockchainDataSource** (implemented by Connectors layer)
```csharp
Task ConnectAsync(CancellationToken ct);
Task DisconnectAsync(CancellationToken ct);
bool IsConnected { get; }
Task SubscribeToTradesAsync(Symbol symbol, CancellationToken ct);
Task UnsubscribeFromTradesAsync(Symbol symbol, CancellationToken ct);
event EventHandler<TradeUpdate>? TradeReceived;
event EventHandler<TradeResponse>? SubscriptionConfirmed;
```

**IBlockchainDataRepository** (implemented by Infrastructure layer)
```csharp
Task AddTradeAsync(TradeUpdate trade, CancellationToken ct);
Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(Symbol symbol, int count, CancellationToken ct);
Task ClearTradesAsync(Symbol symbol, CancellationToken ct);
```

**IBlockchainDataService** (consumed by API layer)
```csharp
Task StartAsync(CancellationToken ct);
Task StopAsync(CancellationToken ct);
Task SubscribeToTradesAsync(Symbol symbol, CancellationToken ct);
Task UnsubscribeFromTradesAsync(Symbol symbol, CancellationToken ct);
Task<IReadOnlyList<TradeUpdate>> GetRecentTradesAsync(Symbol symbol, int count, CancellationToken ct);
event EventHandler<TradeUpdate>? TradeReceived;
```

### BlockchainDataService

The main service that:
1. Connects to the data source on `StartAsync`
2. Subscribes to the `TradeReceived` event from the data source
3. Stores incoming trades in the repository
4. Re-emits `TradeReceived` for real-time consumers (SignalR)
5. Provides `GetRecentTradesAsync` for REST API queries

## Connectors Layer (TODO)

Will implement `IBlockchainDataSource` with:
- WebSocket client connecting to `wss://ws.blockchain.info/mercury-gateway/v1/ws`
- Required header: `Origin: https://exchange.blockchain.com`
- JSON message serialization using `EnumMember` attributes
- Automatic reconnection logic
- Subscription management

## Infrastructure Layer (TODO)

Will implement `IBlockchainDataRepository` with:
- In-memory cache using `ConcurrentDictionary`
- Circular buffer for recent trades (configurable size per symbol)
- Thread-safe operations

## API Layer (TODO)

Will provide:
- REST endpoints for querying cached data
- SignalR hub for real-time trade updates
- Background service to host `BlockchainDataService` lifecycle

## External API Reference

Blockchain.com Exchange WebSocket API: https://exchange.blockchain.com/api

### Key Channels

| Channel | Description |
|---------|-------------|
| `trades` | Real-time trade executions |
| `l2` | Level 2 order book (aggregated) |
| `l3` | Level 3 order book (individual orders) |
| `ticker` | Ticker updates |
| `prices` | Candlestick/OHLC data |

### Message Format

Subscribe request:
```json
{"action": "subscribe", "channel": "trades", "symbol": "BTC-USD"}
```

Trade update response:
```json
{
  "seqnum": 21,
  "event": "updated",
  "channel": "trades",
  "symbol": "BTC-USD",
  "timestamp": "2019-08-13T11:30:06.100140Z",
  "side": "sell",
  "qty": 8.5e-5,
  "price": 11252.4,
  "trade_id": "12884909920"
}
```

## Implementation Roadmap

1. [x] Domain entities and enums
2. [x] Application layer interfaces and service
3. [ ] Infrastructure layer (in-memory repository)
4. [ ] Connectors layer (WebSocket client)
5. [ ] API layer (REST endpoints, SignalR hub)
6. [ ] Integration testing with live API
