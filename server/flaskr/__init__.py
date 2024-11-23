from flask import Flask, render_template
from flask_socketio import SocketIO

# Initialize SocketIO
app = Flask(__name__)
app.config['SECRET_KEY'] = 'secret!'
socketio = SocketIO(app, cors_allowed_origins="*")

def create_app():
    # Initialize SocketIO with the app
    socketio.init_app(app)

    @app.route('/')
    def index():
        return render_template('index.html')
    
    return app
