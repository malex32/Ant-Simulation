using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class AntScript : MonoBehaviour
{
    // State of ant
    public enum State { SearchingForFood, ReturningHome }
    State currentState;

    //Other Scripts
    public AntSettings settings;

    // Masks
    public LayerMask HomeMask;
    public LayerMask FoodMask;

    // Drop Pheromones
    public GameObject pheromonePrefab; // Pheromone prefab for food search
    public GameObject pheromonePrefabFood; // Pheromone prefab for home search
    Vector2 lastPheromonePos;

    // Sense Pheromones
    PerceptionMap.Entry[] pheromoneEntries;
    Vector2[] sensors = new Vector2[3];
	float[] sensorData = new float[3];

    // Food
    private Transform targetFood;
    private GameObject carriedFood;
    Collider2D[] foodColliders;
    float leftFoodTime;

    // Movement
        //Direction
    Vector2 currentForwardDir;
    Vector2 currentPosition;
    float nextDirUpdateTime;
    Vector2 currentVelocity;
        //Random
    float nextRandomSteerTime;
    Vector2 randomSteerForce;
        //Pheromone
    Vector2 pheromoneSteerForce;
        //Turning
    bool turningAround;
    Vector2 turnAroundForce;
	float turnAroundEndTime;

    //Colony 
    float leftHomeTime;
    public Colony colony;

    // New Movement

    Vector2 desiredDirection;
    Vector2 velocity;
    Vector2 position;

    // Collision Detection
    Vector2 leftVect;
    Vector2 rightVect;
    public LayerMask screenBoundary;
    Vector2 obstacleAvoidForce;
    float obstacleForceResetTime;
    enum Antenna { None, Left, Right }
	Antenna lastAntennaCollision;


    void Start()
    {

        currentState = State.SearchingForFood;
        currentPosition = transform.position;
        currentForwardDir = transform.up;
        targetFood = null;
        leftHomeTime = Time.time;
        lastPheromonePos = transform.position;
        foodColliders = new Collider2D[1];
        nextDirUpdateTime = Random.value * settings.timeBetweenDirUpdate;
        const int maxPerceivedPheromones = 1024;
		pheromoneEntries = new PerceptionMap.Entry[maxPerceivedPheromones];
        currentVelocity = currentForwardDir * settings.maxSpeed;

        position = transform.position;
        velocity = currentForwardDir * settings.maxSpeed;

    }

    void Update()
    {
        HandlePheromonePlacement();
        HandleRandomSteering();
        if (currentState == State.SearchingForFood)
        {
            HandleSearchForFood();
        }
        else if (currentState == State.ReturningHome)
        {
            HandleReturnHome();
        }
        HandleMovement();
        HandleCollision();

    }

    void HandleMovement()
    {
        RaycastHit2D hit = Physics2D.Raycast(currentPosition, currentForwardDir, settings.antennaDst, screenBoundary);
		if (hit)
		{
			if (!turningAround)
			{
				StartTurnAround(Vector2.Reflect(currentForwardDir, hit.normal), 2);
                Debug.Log("I'm turning!");
			}
		}

        desiredDirection = randomSteerForce + pheromoneSteerForce + obstacleAvoidForce;
        
        if (turningAround)
        {
            desiredDirection += turnAroundForce * settings.targetSteerStrength;
            if (Time.time > turnAroundEndTime)
            {
                turningAround = false;
            }
        }
        Vector2 desiredVelocity = desiredDirection * settings.maxSpeed;
        Vector2 desiredSteeringForce = (desiredVelocity - velocity) * settings.steerSpeed;
        Vector2 acceleration = Vector2.ClampMagnitude(desiredSteeringForce,settings.steerSpeed);

        velocity = Vector2.ClampMagnitude(velocity + acceleration * Time.deltaTime, settings.maxSpeed);
        position += velocity * Time.deltaTime;

        // Update the ant's position and rotation
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg - 90;
        transform.SetPositionAndRotation(new Vector3(position.x, position.y, transform.position.z), Quaternion.Euler(0, 0, angle));
        currentForwardDir = transform.up; // Ensure the forward direction is updated
    }

    void HandleCollision()
    {
        leftVect = (currentForwardDir + (Vector2)transform.right * settings.sensorDst).normalized;
		rightVect = (currentForwardDir - (Vector2)transform.right * settings.sensorDst).normalized;

        RaycastHit2D hitLeft =  Physics2D.Raycast(position, leftVect, settings.antennaDst, screenBoundary);
        RaycastHit2D hitRight =  Physics2D.Raycast(position, rightVect, settings.antennaDst, screenBoundary);

        if (Time.time > obstacleForceResetTime)
		{
			obstacleAvoidForce = Vector2.zero;
			lastAntennaCollision = Antenna.None;
		}

        if (hitLeft || hitRight)
		{
			if (hitLeft && lastAntennaCollision != Antenna.Right && (!hitRight || hitLeft.distance < hitRight.distance))
			{
				obstacleAvoidForce = -transform.right * settings.collisionAvoidSteerStrength;
				lastAntennaCollision = Antenna.Left;
			}
			if (hitRight && lastAntennaCollision != Antenna.Left && (!hitLeft || hitRight.distance < hitLeft.distance))
			{
				obstacleAvoidForce = transform.right * settings.collisionAvoidSteerStrength;
				lastAntennaCollision = Antenna.Right;
			}

			obstacleForceResetTime = Time.time + 0.5f;
			randomSteerForce = obstacleAvoidForce.normalized * settings.randomSteerStrength;
		}

    }

    void HandlePheromonePlacement()
    {
        if (Vector2.Distance(transform.position, lastPheromonePos) > settings.dstBetweenMarkers)
        {
            Vector3 pheromoneOffset = transform.position - (transform.up * 1.25f);
            if (currentState == State.SearchingForFood)
            {
                float t = 1 - (Time.time - leftHomeTime) / settings.pheromoneRunOutTime;
                t = Mathf.Lerp(0.5f, 1, t);
                GameObject pheromone = Instantiate(pheromonePrefab, pheromoneOffset, Quaternion.identity);
                colony.globalHomeMarkers.Add(pheromoneOffset, t, pheromone);
                lastPheromonePos = (Vector2)transform.position + 0.2f * settings.dstBetweenMarkers * (Vector2)Random.insideUnitCircle;
            }
            else if (currentState == State.ReturningHome)
            {
                float t = 1 - (Time.time - leftFoodTime) / settings.pheromoneRunOutTime;
                t = Mathf.Lerp(0.5f, 1, t);
                GameObject pheromone = Instantiate(pheromonePrefabFood, pheromoneOffset, Quaternion.identity);
                colony.globalFoodMarkers.Add(pheromoneOffset, t, pheromone);
                lastPheromonePos = (Vector2)transform.position + 0.2f * settings.dstBetweenMarkers * (Vector2)Random.insideUnitCircle;
            }
        }
    }

    void HandleRandomSteering()
    {
        if (targetFood != null)
        {
            randomSteerForce = Vector2.zero;
            return;
        }
        if (Time.time > nextRandomSteerTime)
        {
            nextRandomSteerTime = Time.time + Random.Range(settings.randomSteerMaxDuration / 2, settings.randomSteerMaxDuration);
			randomSteerForce = GetRandomDir(currentForwardDir, 5) * settings.randomSteerStrength;
        }
    }

    void HandleSearchForFood()
    {
        if (targetFood == null)
        {
            int numFoodInRadius = Physics2D.OverlapCircleNonAlloc(transform.position, settings.perceptionRadius, foodColliders, FoodMask);
			if (numFoodInRadius > 0)
			{
				targetFood = foodColliders[Random.Range(0, numFoodInRadius)].transform;
			}
        }
        if (targetFood != null)
        {
            Vector2 offsetToFood = targetFood.transform.position - transform.position;
			float dstToFood = offsetToFood.magnitude;
			Vector2 dirToFood = offsetToFood / dstToFood;
			pheromoneSteerForce = dirToFood * settings.targetSteerStrength;
        }
        else
        {
            HandlePheromoneSteering();
        }
    }

    void HandleReturnHome()
    {
        Vector2 currentPos = transform.position;
        Collider2D home = Physics2D.OverlapCircle(transform.position, settings.perceptionRadius, HomeMask);
        if (home)
        {
            pheromoneSteerForce = ((Vector2)home.transform.position - currentPos).normalized * settings.targetSteerStrength;
        }
        else
        {
            HandlePheromoneSteering();
        }
    }

    void HandlePheromoneSteering()
    {
        if (Time.time > nextDirUpdateTime)
        {
            Vector2 currentPos = transform.position;
            
            Vector2 leftSensorDir = (currentForwardDir + (Vector2)transform.right * settings.sensorDst).normalized;
			Vector2 rightSensorDir = (currentForwardDir - (Vector2)transform.right * settings.sensorDst).normalized;


            pheromoneSteerForce = Vector2.zero;
			float currentTime = Time.time;
			const int centreIndex = 0;
			const int leftIndex = 1;
			const int rightIndex = 2;
            nextDirUpdateTime = Time.time + settings.timeBetweenDirUpdate;
			// centre
			sensors[centreIndex] = currentPos + currentForwardDir * settings.sensorDst;
			// left
			sensors[leftIndex] = currentPos + leftSensorDir * settings.sensorDst;
			// right
			sensors[rightIndex] = currentPos + rightSensorDir * settings.sensorDst;

            for (int i = 0; i < 3; i++)
            {
                sensorData[i] = 0;
                int numPheromones = 0;
                if (currentState == State.SearchingForFood)
                {
                    numPheromones = colony.globalFoodMarkers.GetAllInCircle(pheromoneEntries, sensors[i]);
                }
                if (currentState == State.ReturningHome)
                {
                    numPheromones = colony.globalHomeMarkers.GetAllInCircle(pheromoneEntries, sensors[i]);
                }

                for (int j = 0; j < numPheromones; j++)
                {
                    float evaporateT = (currentTime - pheromoneEntries[j].creationTime) / settings.pheromoneEvaporateTime;
					float strength = Mathf.Clamp01(1 - evaporateT);
					sensorData[i] += strength;
                }

            }
            float centre = sensorData[centreIndex];
			float left = sensorData[leftIndex];
			float right = sensorData[rightIndex];

            if (centre > left && centre > right)
			{
				pheromoneSteerForce = currentForwardDir * settings.pheromoneWeight;
			}
			else if (left > right)
			{
				pheromoneSteerForce = leftSensorDir * settings.pheromoneWeight;
			}
			else if (right > left)
			{
				pheromoneSteerForce = rightSensorDir * settings.pheromoneWeight;
            }
        }
    }



    Vector2 GetRandomDir(Vector2 referenceDir, int similarity = 4)
    {
        Vector2 smallestRandomDir = Vector2.zero;
        float change = -1;
        const int iterations = 4; // Increase iterations based on similarity

        for (int i = 0; i < iterations; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            float dot = Vector2.Dot(referenceDir, randomDir);
            if (dot > change)
            {
                change = dot;
                smallestRandomDir = randomDir;
            }
        }
        return smallestRandomDir;
    }


    // Collision with other items
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Colony") && currentState == State.ReturningHome)
        {
            HandleHomeCollision();
        }
        else if (other.CompareTag("Food") && currentState == State.SearchingForFood)
        {
            HandleFoodCollision(other);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Colony") && currentState == State.SearchingForFood)
        {
            leftHomeTime = Time.time;
        }
    }

    void HandleHomeCollision()
    {
        if (carriedFood != null)
        {
            Destroy(carriedFood.gameObject); // Remove the food
            carriedFood = null; // Reset the target
            nextDirUpdateTime = 0;
            leftHomeTime = Time.time;
            StartTurnAround();
            currentState = State.SearchingForFood;
        }
    }

    void HandleFoodCollision(Collider2D other)
    {
        currentState = State.ReturningHome;
        other.transform.parent = transform;
        carriedFood = other.gameObject;
        carriedFood.transform.localPosition = new Vector3(0, 1.5f, 0);
        carriedFood.tag = "PickedUpFood";
        carriedFood.layer = LayerMask.NameToLayer("PickedUpFood");
        nextDirUpdateTime = 0;
        targetFood = null;
        leftFoodTime = Time.time;
        StartTurnAround();
    }

    void StartTurnAround(float randomStrength = 0.2f)
	{
		StartTurnAround(-currentForwardDir, randomStrength);
	}
    void StartTurnAround(Vector2 returnDir, float randomStrength = 0.2f)
	{
		turningAround = true;
		turnAroundEndTime = Time.time + 1.5f;
		Vector2 perpAxis = new Vector2(-returnDir.y, returnDir.x);
		turnAroundForce = returnDir + (Random.value - 0.5f) * 2 * randomStrength * perpAxis;
	}

}
