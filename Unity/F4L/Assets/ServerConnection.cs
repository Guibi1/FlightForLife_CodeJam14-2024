using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using static SocketIOUnity;
using SocketIOClient.Newtonsoft.Json;
using System.Linq;
using static ServerConnection;

public class ServerConnection : MonoBehaviour
{
    private SocketIOUnity socket;

    [SerializeField]
    public List<GameObject> drones;

    [Header("Map")]
    public static float originLatitude = -0.375984f;
    public static float originLongitude = 39.47132f;
    public const float scaleFactor = 0.0001f;


    void Start()
    {
        Debug.Log("Initializing Socket.IO client...");
        var uri = new System.Uri("http://localhost:5000/unity");
        socket = new SocketIOUnity(uri);
        socket.unityThreadScope = UnityThreadScope.Update;
        socket.JsonSerializer = new NewtonsoftJsonSerializer();

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

        socket.OnUnityThread("move_command", response =>
        {
            MovementRequest json = response.GetValue<MovementRequest>();
            Debug.Log("Move_Command from server: " + json);


            Vector2 clickedPos = LngLatToVector(json, originLatitude, originLongitude, scaleFactor);

            GameObject drone = drones.OrderBy(drone => Vector2.Distance(clickedPos, drone.transform.position)).First();
            drone.GetComponent<MoveDrone>().MoveDroneTo(clickedPos, json.id);
        });

        socket.OnUnityThread("abort_move_command", response =>
        {
            AbortMovementRequest json = response.GetValue<AbortMovementRequest>();
            Debug.Log("Abort_Move_Command from server: " + json);

            var drone = drones.Where(drone => drone.GetComponent<MoveDrone>().GetOverrideId() == json.id).First();
            if (drone != null)
            {
                drone.GetComponent<MoveDrone>().ResumeScanMovements();
            }
        });

        socket.OnUnityThread("drone_pause", response =>
        {
            PauseGoRequest json = response.GetValue<PauseGoRequest>();
            Debug.Log("Pause_Command from server: " + json);


            if (drones[json.drone] != null)
            {
                drones[json.drone].GetComponent<MoveDrone>().PauseScanMovements();
            }
        });

        socket.OnUnityThread("drone_go", response =>
        {
            PauseGoRequest json = response.GetValue<PauseGoRequest>();
            Debug.Log("Go_Command from server: " + json);


            if (drones[json.drone] != null)
            {
                drones[json.drone].GetComponent<MoveDrone>().ResumeScanMovements();
            }
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
            List<DroneWebPositionData> droneDataList = new List<DroneWebPositionData>();

            for (int i = 0; i < drones.Count; i++)
            {
                DroneWebPositionData droneData = DroneToWebPos(i, originLatitude,originLongitude,scaleFactor);
                droneDataList.Add(droneData);
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

    public DroneWebPositionData DroneToWebPos(int i, double originLatitude, double originLongitude, double scaleFactor)
    {
        GameObject drone = drones[i];

        // Extract x and y from the DroneData object
        double x = drone.transform.position.x;
        double y = drone.transform.position.y;

        // Convert x, y to latitude and longitude
        double latitude = originLatitude + (y * scaleFactor);
        double longitude = originLongitude + (x * scaleFactor / Math.Cos(originLatitude * Math.PI / 180));

        // Return a new DroneWebPositionData object with converted values
        return new DroneWebPositionData
        {
            id = i, // Default ID or customize as needed
            lng = (float)longitude,
            lat = (float)latitude,
            rotation = drone.transform.eulerAngles.z,
            overriten = drone.GetComponent<MoveDrone>().GetOverrideId() != null,
        };
    }

    public Vector2 LngLatToVector(LngLat ll, double originLatitude, double originLongitude, double scaleFactor)
    {
        if (ll == null)
        {
            throw new ArgumentNullException("LngLat cannot be null.");
        }
        print("ll");
        print(ll.lng);
        print(ll.lat);

        // Convert latitude and longitude to local x, y coordinates
        double y = (ll.lat - originLatitude) / scaleFactor;
        double x = (ll.lng - originLongitude) * Math.Cos(originLatitude * Math.PI / 180) / scaleFactor;

        // Return a new DroneData object with converted values
        return new Vector2((float)x, (float)y);

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

    [System.Serializable]
    public class LngLat
    {
        public float lng;
        public float lat;

    }

    [System.Serializable]
    public class MovementRequest : LngLat
    {
        public string id;
    }

    [System.Serializable]
    public class AbortMovementRequest
    {
        public string id;
    }

    [System.Serializable]
    public class DroneWebPositionData : LngLat
    {
        public int id;
        public float rotation;
        public bool overriten;
    }

    [System.Serializable]
    public class PauseGoRequest
    {
        public int drone;
    }


}
