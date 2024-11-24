using TMPro;
using UnityEngine;

public class SwarmSweep : MonoBehaviour
{
    public float size = 10f;
    public ServerConnection sc;

    void Start()
    {
        double gap = size / sc.drones.Count;

        for (int i = 0; i < sc.drones.Count; i++)
        {
            sc.drones[i].transform.position = new Vector3(0, 10, (float)(gap * (i + 0.5)));
            sc.drones[i].GetComponent<MoveDrone>().StartPath(transform.position + new Vector3(size, 10, (float)(gap * (i + 0.5))));
        }
    }
}
