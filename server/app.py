import json
from flask import Flask, Response
from flask_socketio import SocketIO, Namespace
import requests

# Create Flask app and SocketIO instance
app = Flask(__name__)
socketio = SocketIO(app, cors_allowed_origins="*")


# Dictionary to store drone states and their respective models
drones = {}
alerts = []


# Function to update drone data
def update_drones(drone_id, human_count):
    """Update the state of a drone with the latest human count."""
    drones[drone_id] = {"human_count": human_count}
    print(f"Updated drone {drone_id}: {human_count} human(s) detected.")
    socketio.emit("drone_update", {"drone_id": drone_id, "human_count": human_count})


@app.route("/drone/<int:drone_id>", methods=["GET"])
def get_drone(drone_id):
    """
    Fetch the state of a specific drone by its ID.
    """

    url = "http://localhost:" + (drone_id + 8001) + "/drone"

    res = requests.get(url)
    return Response(res, mimetype="multipart/x-mixed-replace; boundary=frame")


# Namespace for Unity
class UnityNamespace(Namespace):
    def on_connect(self):
        print("Unity client connected to /unity")

    def on_disconnect(self):
        print("Unity client disconnected from /unity")

    def on_message(self, data):
        print(f"Unity /unity 'message' event: {data}")
        socketio.emit("message", data, namespace="/unity")

    def on_positions(self, data):
        data = json.loads(data)
        # print(data, type(data))

        global drones

        for drone in data:
            drone["alert"] = drone["id"] in alerts
            drones[drone["id"]] = drone

        socketio.emit("drones", data, namespace="/frontend")


# WebSocket event handler for client connections


class FrontendNamespace(Namespace):
    def on_connect(self):
        print("Frontend client connected to /frontend")

    def on_disconnect(self):
        print("Frontend client disconnected from /frontend")

    def on_message(self, data):
        print(f"Frontend /frontend 'message' event: {data}")
        socketio.emit("response", data, namespace="/frontend")

    def on_request_movement(self, data):
        print("Frontend: Requesting Movement", data)
        socketio.emit("move_command", data, namespace="/unity")

    def on_abort_movement(self, data):
        print("Frontend: Abort Movement Request", data)
        socketio.emit("abort_move_command", data, namespace="/unity")

    def on_stop_override(self, data):
        print("Frontend: Stop Movement Override ", data)
        socketio.emit("drone_go", data, namespace="/unity")

    def on_dismiss_alert(self, data):
        print("Frontend: Dismissing Alerts", data)
        if data.confirmed:
            socketio.emit(
                "move_command",
                {
                    "lng": drones[data.drone].lng,
                    "lat": drones[data.drone].lat,
                    "id": "rescue" + data.drone,
                },
                namespace="/unity",
            )
        else:
            alerts.remove(data.drone)
            socketio.emit("drone_go", {"drone": data.drone}, namespace="/unity")


class AiNamespace(Namespace):
    def on_connect(self):
        print("Frontend client connected to /frontend")

    def on_disconnect(self):
        print("Frontend client disconnected from /frontend")

    def on_message(self, data):
        print(f"Frontend /frontend 'message' event: {data}")
        socketio.emit("response", data, namespace="/frontend")

    def on_drone_ai_alert(self, drone_id):
        print(f"Unity /unity 'drone_ai_abort' event: {drone_id}")
        socketio.emit("drone_pause", {"drone": drone_id}, namespace="/unity")

    def on_drone_feed(self, data):
        print(data)


socketio.on_namespace(UnityNamespace("/unity"))
socketio.on_namespace(FrontendNamespace("/frontend"))
socketio.on_namespace(FrontendNamespace("/ai"))


if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
