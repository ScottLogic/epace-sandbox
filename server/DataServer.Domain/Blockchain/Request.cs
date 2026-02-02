namespace DataServer.Domain.Blockchain;

public record Request(SubscriptionAction Action, Channel Channel, Symbol? Symbol = null);
