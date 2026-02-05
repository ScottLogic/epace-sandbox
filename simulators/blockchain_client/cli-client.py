import asyncio
import json
import os
import sys

from dotenv import load_dotenv
from signalrcore.hub_connection_builder import HubConnectionBuilder

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))
from disconnect_controls import DisconnectController, DisconnectMode

load_dotenv()

SYMBOLS = ["ETH-USD", "BTC-USD"]
SIGNALR_URL = os.getenv("SIGNALR_URL")
BLOCKCHAIN_URL = f"{SIGNALR_URL}/blockchain"


def print_separator():
    print("\n" + "=" * 60 + "\n")


def print_json(data: dict | str, label: str = "Response"):
    print_separator()
    print(f"[{label}]")
    if isinstance(data, str):
        try:
            parsed = json.loads(data)
            print(json.dumps(parsed, indent=2))
        except json.JSONDecodeError:
            print(data)
    else:
        print(json.dumps(data, indent=2))


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


def create_subscribe_request(symbol: str) -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "subscribe",
        "params": {"channel": "trades", "symbol": symbol},
        "id": "1",
    }
    return json.dumps(request)


def create_unsubscribe_request(symbol: str) -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "unsubscribe",
        "params": {"channel": "trades", "symbol": symbol},
        "id": "2",
    }
    return json.dumps(request)


def create_malformed_request() -> str:
    request = {
        "jsonrpc": "2.0",
        "method": "subscribe",
        "params": {"channel": "invalid_channel", "symbol": "INVALID-SYMBOL"},
        "id": "bad-request",
    }
    return json.dumps(request)


def on_message_received(message):
    print_json(message, "Message Received")


def on_error(error):
    print(f"\n[Error] {error}")


def on_close():
    print("\n[Connection Closed]")


def on_open():
    print("\n[Connection Opened]")


def build_hub_connection():
    hub_connection = (
        HubConnectionBuilder()
        .with_url(BLOCKCHAIN_URL)
        .with_automatic_reconnect(
            {
                "type": "raw",
                "keep_alive_interval": 10,
                "reconnect_interval": 5,
                "max_attempts": 5,
            }
        )
        .build()
    )
    hub_connection.on_open(on_open)
    hub_connection.on_close(on_close)
    hub_connection.on_error(on_error)
    hub_connection.on("ReceiveMessage", on_message_received)
    return hub_connection


class SignalRDisconnectController(DisconnectController):
    def __init__(self, hub_connection, symbol: str):
        super().__init__()
        self.hub = hub_connection
        self.symbol = symbol

    async def on_graceful_disconnect(self):
        print("\n[Graceful Disconnect] Unsubscribing and closing connection...")
        try:
            unsubscribe_request = create_unsubscribe_request(self.symbol)
            self.hub.send("SendMessage", [unsubscribe_request])
            await asyncio.sleep(0.5)
            self.hub.stop()
            print("[Graceful Disconnect] Connection closed cleanly")
        except Exception as e:
            print(f"[Graceful Disconnect] Error: {e}")

    async def on_abrupt_disconnect(self):
        print("\n[Abrupt Disconnect] Terminating connection immediately...")
        try:
            if hasattr(self.hub, "_transport") and self.hub._transport:
                self.hub._transport.close()
            else:
                self.hub.stop()
            print("[Abrupt Disconnect] Connection terminated")
        except Exception as e:
            print(f"[Abrupt Disconnect] Error: {e}")

    async def on_temporary_drop(self, delay_seconds: int):
        print(f"\n[Temporary Drop] Dropping connection for {delay_seconds} seconds...")
        try:
            if hasattr(self.hub, "_transport") and self.hub._transport:
                self.hub._transport.close()
            else:
                self.hub.stop()
            print("[Temporary Drop] Connection dropped")
        except Exception as e:
            print(f"[Temporary Drop] Error: {e}")

    async def on_reconnect(self):
        print("\n[Reconnect] Rebuilding connection...")
        try:
            self.hub = build_hub_connection()
            self.hub.start()
            await asyncio.sleep(1)
            subscribe_request = create_subscribe_request(self.symbol)
            self.hub.send("SendMessage", [subscribe_request])
            print(f"[Reconnect] Reconnected and resubscribed to {self.symbol}")
        except Exception as e:
            print(f"[Reconnect] Error: {e}")


async def connect_and_subscribe_with_controls(symbol: str):
    hub_connection = build_hub_connection()
    controller = SignalRDisconnectController(hub_connection, symbol)

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        subscribe_request = create_subscribe_request(symbol)
        print_json(subscribe_request, "Sending Subscribe Request")
        hub_connection.send("SendMessage", [subscribe_request])

        print(f"\nSubscribed to trades for {symbol}")
        print("Listening for trade updates...")
        print_separator()

        controller.input_handler.show_commands()

        while controller._running:
            user_input = await asyncio.to_thread(
                lambda: input() if sys.stdin.isatty() else ""
            )
            if user_input:
                mode = DisconnectMode.from_key(user_input.strip())
                if mode:
                    await controller.handle_command(mode)
                    if mode in (
                        DisconnectMode.GRACEFUL,
                        DisconnectMode.ABRUPT,
                        DisconnectMode.QUIT,
                    ):
                        break
                else:
                    print(f"Unknown command: {user_input}")
                    controller.input_handler.show_commands()

    except KeyboardInterrupt:
        print("\n\nInterrupted by user, performing graceful disconnect...")
        await controller.on_graceful_disconnect()
    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


async def connect_and_subscribe(symbol: str):
    hub_connection = build_hub_connection()

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        subscribe_request = create_subscribe_request(symbol)
        print_json(subscribe_request, "Sending Subscribe Request")
        hub_connection.send("SendMessage", [subscribe_request])

        print(f"\nSubscribed to trades for {symbol}")
        print("Listening for trade updates... (Press Ctrl+C to stop)")
        print_separator()

        while True:
            await asyncio.sleep(1)

    except KeyboardInterrupt:
        print("\n\nUnsubscribing and disconnecting...")
        unsubscribe_request = create_unsubscribe_request(symbol)
        hub_connection.send("SendMessage", [unsubscribe_request])
        await asyncio.sleep(0.5)
        hub_connection.stop()
        print("Disconnected by user")
    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


async def send_malformed_request():
    hub_connection = build_hub_connection()

    try:
        hub_connection.start()
        print(f"\nConnecting to {BLOCKCHAIN_URL}...")

        await asyncio.sleep(1)

        malformed_request = create_malformed_request()
        print_json(malformed_request, "Sending Malformed Request")
        hub_connection.send("SendMessage", [malformed_request])

        await asyncio.sleep(2)

        hub_connection.stop()
        print("\nDisconnected after receiving error response")

    except Exception as e:
        print(f"\nError: {e}")
        print(f"Make sure the server is running at {BLOCKCHAIN_URL}")
        hub_connection.stop()


def show_menu() -> str:
    print("\nOptions:")
    print("  1. Subscribe to trades (with disconnect controls)")
    print("  2. Subscribe to trades (simple mode)")
    print("  3. Send malformed request (test error handling)")
    print("  4. Exit")

    while True:
        choice = input("\nSelect an option (1-4): ").strip()
        if choice in ["1", "2", "3", "4"]:
            return choice
        print("Invalid choice. Please enter 1, 2, 3, or 4.")


def main():
    print("=" * 60)
    print("  Blockchain API SignalR Test Client")
    print("=" * 60)

    while True:
        choice = show_menu()

        if choice == "1":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...")
            asyncio.run(connect_and_subscribe_with_controls(symbol))
        elif choice == "2":
            symbol = select_symbol()
            print(f"\nYou selected: {symbol}")
            input("Press Enter to connect and subscribe...")
            asyncio.run(connect_and_subscribe(symbol))
        elif choice == "3":
            input("Press Enter to send a malformed request...")
            asyncio.run(send_malformed_request())
        elif choice == "4":
            print("\nGoodbye!")
            break


if __name__ == "__main__":
    main()
