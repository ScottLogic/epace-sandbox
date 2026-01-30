from flask import Flask, jsonify

app = Flask(__name__)


@app.route("/data", methods=["GET"])
def get_data():
    mock_data = {
        "blocks": [
            {"id": 1, "hash": "0x1a2b3c", "timestamp": "2026-01-30T10:00:00Z"},
            {"id": 2, "hash": "0x4d5e6f", "timestamp": "2026-01-30T10:05:00Z"},
            {"id": 3, "hash": "0x7g8h9i", "timestamp": "2026-01-30T10:10:00Z"},
        ],
        "total_blocks": 3,
    }
    return jsonify(mock_data)


if __name__ == "__main__":
    app.run(debug=True, port=5001)
