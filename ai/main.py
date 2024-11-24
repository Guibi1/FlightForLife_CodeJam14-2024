import threading
import requests
from flask import Flask, Response
import cv2
from ultralytics import YOLO
import base64
import os

app = Flask(__name__)

# Load a YOLO model
model = YOLO("yolo11m.pt")  # Replace with your model
frame_with_humain_box = None

# Define connection URL
SERVER_ALERT_ENDPOINT = "http://host.docker.internal/alert"


def encode_frame(frame):
    """Convert CV2 frame to base64 string"""
    _, buffer = cv2.imencode(".jpg", frame)
    frame_bytes = base64.b64encode(buffer)
    frame_string = frame_bytes.decode("utf-8")
    return frame_string


def filter_and_display(frame, detections):
    """
    Filter detections to show only 'person' and display them on the frame.
    """
    global frame_with_humain_box
    human_detected = False
    for box in detections:
        if int(box.cls[0]) == 0:  # Class ID for 'person' is 0 in YOLO's COCO dataset
            x1, y1, x2, y2 = map(int, box.xyxy[0])
            cv2.rectangle(frame, (x1, y1), (x2, y2), (0, 255, 0), 2)
            cv2.putText(
                frame,
                "Person",
                (x1, y1 - 10),
                cv2.FONT_HERSHEY_SIMPLEX,
                0.5,
                (0, 255, 0),
                2,
            )
            human_detected = True

    # If a human is detected, send data to the Socket.IO server
    if human_detected:
        print("Sending")
        frame_with_humain_box = frame
        frame_string = encode_frame(frame)
        try:
            requests.post(SERVER_ALERT_ENDPOINT, json={"drone": int(os.getenv("DRONE") or 0), "frame": frame_string})
        except:
            print("Couldn't send")

    return frame


@app.route("/drone", methods=["GET"])
def get_drone():
    if "frame_with_humain_box" in globals() and frame_with_humain_box is not None:
        ret, buffer = cv2.imencode(".jpg", frame_with_humain_box)
        frame_data = buffer.tobytes()

        return Response(frame_data, mimetype="image/jpeg")
    else:
        return Response()

def model_inference():
    global frame
    results = model.track(source=os.getenv("SOURCE_URL"), stream=True)
    for result in results:
        frame = result.orig_img
        detections = result.boxes
        if detections:
            frame = filter_and_display(frame, detections)


if __name__ == "__main__":
    # Create a thread for model inference
    inference_thread = threading.Thread(target=model_inference)
    inference_thread.daemon = (
        True  # Allow the thread to exit when the main program exits
    )
    inference_thread.start()

    # Run the Flask app
    app.run(host="0.0.0.0", port=8000)
