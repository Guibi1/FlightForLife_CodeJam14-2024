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


# YOLO model initialization (modify if different per drone)
def create_inference_model():
    model = YOLO("yolo11n.pt")  # Load your YOLO model here
    device = "cuda" if torch.cuda.is_available() else "cpu"
    model.to(device)
    return model


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


@app.route("/drone/<string:drone_id>", methods=["GET"])
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
        socketio.emit("drones", data, namespace="/frontend")

    def on_drone_feed(self, data):
        """
        Handle incoming feed from a drone.
        Data should include:
        - `drone_id`: Unique identifier for the drone.
        - `frame`: Current video frame data for inference.
        """
        drone_id = data.get("drone_id")
        frame = data.get("frame")  # Replace with actual frame decoding logic

        if not drone_id or frame is None:
            socketio.emit("error", {"message": "Invalid data"}, namespace="/unity")
            return

        # Ensure the drone has a model assigned
        if drone_id not in drones:
            print(f"Creating model for drone {drone_id}.")
            drones[drone_id] = {"model": create_inference_model(), "human_count": 0}

        # Perform inference on the frame
        model = drones[drone_id]["model"]
        results = model(frame)  # Assuming frame is preprocessed appropriately
        human_count = sum(
            1 for box in results.boxes if box.cls == 0
        )  # Adjust for human class

        # Update the drone's state with the human count
        update_drones(drone_id, human_count)


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


socketio.on_namespace(UnityNamespace("/unity"))
socketio.on_namespace(FrontendNamespace("/frontend"))


if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
