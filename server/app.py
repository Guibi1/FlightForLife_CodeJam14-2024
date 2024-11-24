import json
from flask import Flask, Response, request, stream_with_context
from flask_socketio import SocketIO, Namespace
from flask_cors import CORS
import requests
import openai_vision

# Create Flask app and SocketIO instance
app = Flask(__name__)
CORS(app, origins="*")
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
    Proxy the stream of a specific drone by its ID.
    """
    url = f"http://localhost:{drone_id + 8001}/drone"

    # Perform the request to the second server
    res = requests.get(url, stream=True)

    # Use stream_with_context to relay the streamed content
    def generate():
        for chunk in res.iter_content(chunk_size=1024):
            if chunk:
                yield chunk

    return Response(
        stream_with_context(generate()),
        content_type="multipart/x-mixed-replace; boundary=frame",
    )


@app.route("/alert", methods=["POST"])
def post_alert():
    data = request.json
    print("Got alert for " + str(data["drone"]))

    if data["drone"] in alerts:
        # openai_vision.getGPTResponseToHelper(data["frame"])
        socketio.emit("drone_pause", {"drone": data["drone"]}, namespace="/unity")
    else:
        alerts.append(data["drone"])

    return Response()


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
        #print(data, type(data))

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
        if data["confirmed"]:
            socketio.emit(
                "move_command",
                {
                    "lng": drones[data["drone"]].lng,
                    "lat": drones[data["drone"]].lat,
                    "id": "rescue" + data["drone"],
                },
                namespace="/unity",
            )
        else:
            alerts.remove(data["drone"])
            socketio.emit("drone_go", {"drone": data["drone"]}, namespace="/unity")


socketio.on_namespace(UnityNamespace("/unity"))
socketio.on_namespace(FrontendNamespace("/frontend"))


if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=80, debug=True)
