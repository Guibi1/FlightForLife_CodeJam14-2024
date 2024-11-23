using UnityEngine;
using SocketIOClient;

public class ServerConnection : MonoBehaviour
{
    private SocketIOUnity socket;

    void Start()
    {
        // Set up the Socket.IO client options
        var uri = new System.Uri("http://localhost:3000"); // Replace with your server URL
        socket = new SocketIOUnity(uri, new SocketIOOptions
        {
            Query = new System.Collections.Generic.Dictionary<string, string>
            {
                { "token", "UNITY" }
            },
            Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
        });

        // Register event handlers
        socket.OnConnected += Socket_OnConnected;
        socket.OnDisconnected += Socket_OnDisconnected;
        socket.On("message", response =>
        {
            Debug.Log("Received message: " + response.GetValue<string>());
        });

        // Connect to the server
        socket.Connect();
    }

    private void Socket_OnConnected(object sender, System.EventArgs e)
    {
        Debug.Log("Socket.IO connected!");

        // Send a message to the server
        socket.Emit("message", "Hello from Unity!");
    }

    private void Socket_OnDisconnected(object sender, string e)
    {
        Debug.Log("Socket.IO disconnected: " + e);
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            // Disconnect and dispose of the socket
            socket.Disconnect();
            socket.Dispose();
        }
    }
}
