from flask import Flask
from flask_sock import Sock

app = Flask(__name__)
sock = Sock(app)


@sock.route("/data")
def data_websocket(ws):
    while True:
        message = ws.receive()
        if message == "subscribe":
            ws.send("connected")


if __name__ == "__main__":
    app.run(debug=True, port=5001)
