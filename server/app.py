from flask import Flask, Response, jsonify
from flask_socketio import SocketIO, Namespace
import torch
import cv2
from ultralytics import YOLO

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


def generate_frames():
    # Capture video from the default camera (0). Replace with a video file path if needed.
    camera = cv2.VideoCapture(0)

    while True:
        success, frame = camera.read()
        if not success:
            break
        else:
            # Encode the frame as JPEG
            ret, buffer = cv2.imencode(".jpg", frame)
            frame = buffer.tobytes()

            # Yield the frame in the HTTP response
            yield (b"--frame\r\n" b"Content-Type: image/jpeg\r\n\r\n" + frame + b"\r\n")


@app.route("/drone/<int:drone_id>", methods=["GET"])
def get_drone(drone_id):
    """
    Fetch the state of a specific drone by its ID.
    """

    # return get request from docker ai

    drone_data = drones[drone_id]
    if drone_data:
        return Response(
            generate_frames(), mimetype="multipart/x-mixed-replace; boundary=frame"
        )
    else:
        # Return an error message within the expected byte format
        error_frame = (
            b"--frame\r\n"
            b"Content-Type: text/plain\r\n\r\n"
            b"Error: Drone with ID '%s' not found\r\n" % drone_id.encode()
        )
        return Response(
            error_frame,
            mimetype="multipart/x-mixed-replace; boundary=frame",
            status=404,
        )


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
        for drone in data:
            drone.alert = drone.id in alerts

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
        print("Frontend: Requesting Movement", data, type(data))
        socketio.emit("move_command", data, namespace="/unity")

    def on_abort_movement(self, data):
        print("Frontend: Requesting Movement", data, type(data))
        socketio.emit("abort_move_command", data, namespace="/unity")


class AiNamespace(Namespace):
    def on_connect(self):
        print("Frontend client connected to /frontend")

    def on_disconnect(self):
        print("Frontend client disconnected from /frontend")

    def on_message(self, data):
        print(f"Frontend /frontend 'message' event: {data}")
        socketio.emit("response", data, namespace="/frontend")

    def on_drone_ai_abort(self, drone_id):
        print(f"Unity /unity 'drone_ai_abort' event: {drone_id}")
        socketio.emit("drone_ai_abort_command", drone_id, namespace="/unity")


    def on_drone_feed(self, data):
        print(data)


socketio.on_namespace(UnityNamespace("/unity"))
socketio.on_namespace(FrontendNamespace("/frontend"))
socketio.on_namespace(FrontendNamespace("/ai"))


if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
