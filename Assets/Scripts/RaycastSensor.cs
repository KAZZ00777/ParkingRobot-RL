using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class RaycastSensor : MonoBehaviour
{
    public float sensorLength = 10f;

    public float[] distances = new float[8];

    public float sensorHeight = 0.5f;

    void Update()
    {
        CastSensors();
    }

    void CastSensors()
    {
        Vector3[] directions =
        {
            transform.forward, //front
            transform.right,//right
            -transform.right, //left
            -transform.forward,//back   
            (transform.forward + transform.right).normalized,//front right
            (transform.forward - transform.right).normalized,//front left
            (-transform.forward + transform.right).normalized,//back right
            (-transform.forward - transform.right).normalized//back left
        };

        String[] sensorNames =
        {
            "Front",
            "Right",
            "Left",
            "Back", 
            "FrontRight",
            "FrontLeft",
            "BackRight",
            "BackLeft"
        };

        for(int i = 0; i < directions.Length; i++)
        {
            RaycastHit hit;

            Vector3 origin = transform.position + Vector3.up * sensorHeight;

            if(Physics.Raycast(origin, directions[i], out hit, sensorLength))
            {
                distances[i] = hit.distance;

                Debug.DrawRay(origin, directions[i] * hit.distance, Color.red);
            }
            else
            {
                distances[i] = sensorLength;

                Debug.DrawRay(origin, directions[i] * sensorLength, Color.green);
            }

            Debug.Log(sensorNames[i] + " = " + distances[i]);

        }

        
    }
}   
