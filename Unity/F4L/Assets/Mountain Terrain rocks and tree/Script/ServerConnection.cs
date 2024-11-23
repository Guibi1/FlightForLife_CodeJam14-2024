using UnityEngine;
using SocketIOClient;

public class ServerConnection : MonoBehaviour
{
    private SocketIOUnity socket;

    void Start()
    {
        Debug.Log("Initializing Socket.IO client...");
        var uri = new System.Uri("http://localhost:5000/unity");
        socket = new SocketIOUnity(uri);

        socket.OnConnected += (sender, e) =>
        {
            Debug.Log("Connected to Socket.IO server!");

            SendMessageToServer("Hello from Unity!");
        };

        socket.OnDisconnected += (sender, e) =>
        {
            Debug.Log("Disconnected from server: " + e);
        };

        socket.On("message", response =>
        {
            Debug.Log("Message from server: " + response.GetValue<string>());
        });

        Debug.Log("Attempting to connect...");
        socket.Connect();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessageToServer("Space key pressed in Unity!");
        }
    }

    // Function to emit a message to the server
    public void SendMessageToServer(string message)
    {
        if (socket.Connected)
        {
            Debug.Log($"Sending message to server: {message}");
            socket.Emit("message", message);
        }
        else
        {
            Debug.LogWarning("Cannot send message. Not connected to server.");
        }
    }

}
