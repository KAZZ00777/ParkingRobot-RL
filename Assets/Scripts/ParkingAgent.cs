using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class ParkingAgent : Agent
{
    [Header("Car Settings")]
    public Rigidbody carRb;
    public float motorForce = 8f;
    public float steerForce = 80f;
    public float maxSpeed = 5f;

    [Header("Sensor")]
    public RaycastSensor raycastSensor;

    [Header("Parking Target")]
    public Transform parkingTarget;
    public float successDistance = 0.7f;
    public float successAngle = 10f;
    public float successSpeed = 0.5f;

    [Header("Start Point")]
    public Transform startPoint;
    public float randomPositionRange = 2f;
    public float randomAngleRange = 45f;

    [Header("Episode Settings")]
    public float maxDistanceFromTarget = 20f;

    private float previousDistance;

    public override void Initialize()
    {
        if (carRb == null)
        {
            carRb = GetComponent<Rigidbody>();
        }

        if (raycastSensor == null)
        {
            raycastSensor = GetComponent<RaycastSensor>();
        }
    }

    public override void OnEpisodeBegin()
    {
        if (carRb != null)
        {
            carRb.linearVelocity = Vector3.zero;
            carRb.angularVelocity = Vector3.zero;
        }

        if (startPoint != null)
        {
            float randomX = Random.Range(-randomPositionRange, randomPositionRange);
            float randomZ = Random.Range(-randomPositionRange, randomPositionRange);
            float randomYaw = Random.Range(-randomAngleRange, randomAngleRange);

            transform.position = startPoint.position + new Vector3(randomX, 0.2f, randomZ);
            transform.rotation = startPoint.rotation * Quaternion.Euler(0f, randomYaw, 0f);
        }

        if (parkingTarget != null)
        {
            previousDistance = Vector3.Distance(transform.position, parkingTarget.position);
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // 1. 距離センサ8個
        if (raycastSensor != null)
        {
            raycastSensor.CastSensors();

            float[] distances = raycastSensor.GetAllNormalizedDistances();

            for (int i = 0; i < distances.Length; i++)
            {
                sensor.AddObservation(distances[i]);
            }
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                sensor.AddObservation(1.0f);
            }
        }

        // 2. 目標駐車位置との相対位置 x, z
        if (parkingTarget != null)
        {
            Vector3 localTarget = transform.InverseTransformPoint(parkingTarget.position);
            sensor.AddObservation(localTarget.x / 10f);
            sensor.AddObservation(localTarget.z / 10f);

            // 3. 目標向きとの角度差
            float angleDiff = Vector3.SignedAngle(transform.forward, parkingTarget.forward, Vector3.up);
            sensor.AddObservation(angleDiff / 180f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }

        // 4. 車のローカル速度 x, z
        if (carRb != null)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(carRb.linearVelocity);
            sensor.AddObservation(localVelocity.x / maxSpeed);
            sensor.AddObservation(localVelocity.z / maxSpeed);

            // 5. 車の回転速度 y
            sensor.AddObservation(carRb.angularVelocity.y / 10f);
        }
        else
        {
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
            sensor.AddObservation(0f);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float steer = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float throttle = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        MoveCar(steer, throttle);
        GiveReward();
    }

    private void MoveCar(float steer, float throttle)
    {
        if (carRb == null)
        {
            return;
        }

        if (carRb.linearVelocity.magnitude < maxSpeed)
        {
            Vector3 force = transform.forward * throttle * motorForce;
            carRb.AddForce(force, ForceMode.Acceleration);
        }

        float speedRate = Mathf.Clamp01(carRb.linearVelocity.magnitude / maxSpeed);

        Vector3 torque = Vector3.up * steer * steerForce * speedRate;
        carRb.AddTorque(torque, ForceMode.Acceleration);
    }

    private void GiveReward()
    {
        if (parkingTarget == null || carRb == null)
        {
            AddReward(-0.001f);
            return;
        }

        float currentDistance = Vector3.Distance(transform.position, parkingTarget.position);
        float angleDiff = Mathf.Abs(Vector3.SignedAngle(transform.forward, parkingTarget.forward, Vector3.up));
        float speed = carRb.linearVelocity.magnitude;

        // 目標に近づいたら報酬、遠ざかったら罰
        float improvement = previousDistance - currentDistance;
        AddReward(improvement * 0.5f);
        previousDistance = currentDistance;

        // 毎ステップ少し罰。無駄な行動を減らす
        AddReward(-0.001f);

        // 目標方向に向いているほど少し報酬
        float angleReward = 1f - (angleDiff / 180f);
        AddReward(angleReward * 0.001f);

        // 成功条件
        bool isCloseEnough = currentDistance < successDistance;
        bool isAngleGood = angleDiff < successAngle;
        bool isSlowEnough = speed < successSpeed;

        if (isCloseEnough && isAngleGood && isSlowEnough)
        {
            AddReward(3f);
            EndEpisode();
        }

        // 遠くに行きすぎたら失敗
        if (currentDistance > maxDistanceFromTarget)
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall") ||
            collision.gameObject.CompareTag("Obstacle"))
        {
            AddReward(-1f);
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> actions = actionsOut.ContinuousActions;

        actions[0] = Input.GetAxis("Horizontal"); // A/D または ←/→
        actions[1] = Input.GetAxis("Vertical");   // W/S または ↑/↓
    }
}
