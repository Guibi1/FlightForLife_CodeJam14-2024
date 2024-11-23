using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;

public class ServerConnection : MonoBehaviour
{
    private SocketIOUnity socket;

    [SerializeField]
    private GameObject[] drones;


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
        SendDronePositionsToServer();
    }

    public void SendDronePositionsToServer()
    {
        if (socket.Connected)
        {
            Dictionary<string, DroneData> droneDataList = new Dictionary<string, DroneData>();

            for (int i=0;i<drones.Length;i++)
            {
                // Extract data from each drone
                string droneId = i.ToString(); // Use the name of the GameObject as the ID
                Vector3 position = drones[i].transform.position;
                float rotation = drones[i].transform.eulerAngles.y;

                // Populate the DroneData object
                DroneData droneData = new DroneData
                {
                    pos = new PositionData
                    {
                        x = position.x,
                        y = position.y,
                        z = position.z,
                        rotation = rotation
                    }
                };

                // Add to the dictionary with the drone ID as the key
                droneDataList[droneId] = droneData;
            }

            string json = JsonConvert.SerializeObject(droneDataList, Formatting.Indented);
            socket.Emit("positions", json);

        }
        else
        {
            Debug.LogWarning("Cannot send positions. Not connected to server.");
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

    [System.Serializable]
    public class PositionData
    {
        public float x;
        public float y;
        public float z;
        public float rotation;
    }

    [System.Serializable]
    public class DroneData
    {
        public PositionData pos;
    }

}

