from flaskr import create_app, socketio

# Create Flask app instance
app = create_app()

# WebSocket event handler
@socketio.on('message')
def handle_message(data):
    print(f"Received message: {data}")
    socketio.emit('response', {'data': f"Server received: {data['data']}"})

if __name__ == '__main__':
    # Run the app with SocketIO
    socketio.run(app, debug=True)

drones = {}
def update_drones(drone_id, update):
    drones[drone_id] = update

    


    
