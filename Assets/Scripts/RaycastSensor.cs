using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastSensor : MonoBehaviour
{
public float sensorLength = 20f;

public float[] distances = new float[8];

void Update()
{
CastSensors();
}

void CastSensors()
{
Vector3[] directions =
{
transform.forward,
(transform.forward + transform.right).normalized,
transform.right,
(-transform.forward + transform.right).normalized,
-transform.right,
(transform.forward - transform.right).normalized,
-transform.forward,
(-transform.forward - transform.right).normalized
};

for(int i = 0; i < directions.Length; i++)
{
RaycastHit hit;

if(Physics.Raycast(transform.position, directions[i], out hit, sensorLength))
{
distances[i] = hit.distance;

Debug.DrawRay(transform.position, directions[i] * hit.distance, Color.red);
}
else
{
distances[i] = sensorLength;

Debug.DrawRay(transform.position, directions[i] * sensorLength, Color.green);
}

Debug.Log("i = " + distances[i]);

}

}
}
