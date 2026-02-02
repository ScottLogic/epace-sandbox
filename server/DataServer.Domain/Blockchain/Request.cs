namespace DataServer.Domain.Blockchain;

public record Request(Action Action, Channel Channel, Symbol? Symbol = null);
