from flask import Flask, jsonify
from flask_socketio import SocketIO, Namespace
import torch
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


# Namespace for Unity
class UnityNamespace(Namespace):
    def on_connect(self):
        print("Unity client connected to /unity")

    def on_disconnect(self):
        print("Unity client disconnected from /unity")

    def on_message(self,data):
        print(f"Frontend /frontend 'message' event: {data}")
        socketio.emit('message', data, namespace='/unity')

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
        human_count = sum(1 for box in results.boxes if box.cls == 0)  # Adjust for human class

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
        socketio.emit('response', data, namespace='/frontend')


socketio.on_namespace(UnityNamespace("/unity"))
socketio.on_namespace(FrontendNamespace('/frontend'))



if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
