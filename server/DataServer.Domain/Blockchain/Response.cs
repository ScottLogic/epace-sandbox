namespace DataServer.Domain.Blockchain;

public record Response(int Seqnum, Event Event, Channel Channel, Symbol? Symbol = null);
