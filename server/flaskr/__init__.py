from flask import Flask, render_template
from flask_socketio import SocketIO

# Initialize SocketIO
socketio = SocketIO()

def create_app():
    # Create Flask app instance
    app = Flask(__name__)
    app.config['SECRET_KEY'] = 'secret!'
    
    # Initialize SocketIO with the app
    socketio.init_app(app)

    @app.route('/')
    def index():
        return render_template('index.html')
    
    return app
