namespace DataServer.Domain.Blockchain;

public record TradeUpdate(
    int Seqnum,
    Event Event,
    Channel Channel,
    Symbol Symbol,
    DateTimeOffset Timestamp,
    string Side,
    decimal Qty,
    decimal Price,
    string TradeId
);
