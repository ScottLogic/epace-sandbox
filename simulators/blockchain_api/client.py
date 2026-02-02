import asyncio
import json
import websockets


SYMBOLS = ["ETH-USD", "BTC-USD"]
WEBSOCKET_URL = "ws://localhost:5000/ws"


def print_separator():
    print("\n" + "=" * 60 + "\n")


def print_json(data: dict, label: str = "Response"):
    print_separator()
    print(f"[{label}]")
    print(json.dumps(data, indent=2))


async def connect_and_subscribe(symbol: str):
    request = {
        "action": "subscribe",
        "channel": "trades",
        "symbol": symbol
    }
    
    print_json(request, "Sending Request")
    
    try:
        async with websockets.connect(WEBSOCKET_URL) as ws:
            await ws.send(json.dumps(request))
            
            print(f"\nConnected to {WEBSOCKET_URL}")
            print(f"Subscribed to trades for {symbol}")
            print("Listening for trade updates... (Press Ctrl+C to stop)")
            print_separator()
            
            while True:
                message = await ws.recv()
                data = json.loads(message)
                print_json(data, "Trade Update")
                
    except websockets.exceptions.ConnectionClosed:
        print("\nConnection closed by server")
    except ConnectionRefusedError:
        print(f"\nError: Could not connect to {WEBSOCKET_URL}")
        print("Make sure the Flask server is running with 'flask run'")
    except KeyboardInterrupt:
        print("\n\nDisconnected by user")


def select_symbol() -> str:
    print("\nAvailable symbols:")
    for i, symbol in enumerate(SYMBOLS, 1):
        print(f"  {i}. {symbol}")
    
    while True:
        try:
            choice = input("\nSelect a symbol (1 or 2): ").strip()
            index = int(choice) - 1
            if 0 <= index < len(SYMBOLS):
                return SYMBOLS[index]
            print("Invalid choice. Please enter 1 or 2.")
        except ValueError:
            print("Invalid input. Please enter a number.")


def main():
    print("=" * 60)
    print("  Blockchain API WebSocket Test Client")
    print("=" * 60)
    
    symbol = select_symbol()
    print(f"\nYou selected: {symbol}")
    input("Press Enter to connect and subscribe...")
    
    asyncio.run(connect_and_subscribe(symbol))


if __name__ == "__main__":
    main()
