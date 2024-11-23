from flask import Flask, jsonify
from flask_socketio import SocketIO
import torch
from ultralytics import YOLO

# Create Flask app and SocketIO instance
app = Flask(__name__)
socketio = SocketIO(app)
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

# WebSocket event handler for incoming drone feeds
@socketio.on("drone_feed")
def handle_drone_feed(data):
    """
    Handle incoming feed from a drone.
    Data should include:
    - `drone_id`: Unique identifier for the drone.
    - `frame`: Current video frame data for inference.
    """
    drone_id = data.get("drone_id")
    frame = data.get("frame")  # Replace with actual frame decoding logic

    if not drone_id or frame is None:
        return jsonify({"error": "Invalid data"}), 400

    # Ensure the drone has a model assigned
    if drone_id not in drones:
        print(f"Creating model for drone {drone_id}.")
        drones[drone_id] = {"model": create_inference_model(), "human_count": 0}

    # Perform inference on the frame
    model = drones[drone_id]["model"]
    results = model(frame)
    human_count = sum(1 for box in results.boxes if box.cls == 0)  # Adjust for human class

    # Update the drone's state with the human count
    update_drones(drone_id, human_count)

# WebSocket event handler for client connections
@socketio.on("connect")
def handle_connect():
    print("Client connected.")
    socketio.emit("welcome", {"message": "Connected to the server!"})

# WebSocket event handler for client disconnections
@socketio.on("disconnect")
def handle_disconnect():
    print("Client disconnected.")

@socketio.on('message')
def handle_message(message):
    print(f"Received message from client: {message}")
    socketio.emit('response', {'data': f"Server received: {message['data']}"})  # Echo message

if __name__ == "__main__":
    # Run the Flask app with SocketIO
    socketio.run(app, host="0.0.0.0", port=5000, debug=True)
