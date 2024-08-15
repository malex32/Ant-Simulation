using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu()]
public class AntSettings : ScriptableObject
{
    [Header("Movement")]
    public float maxSpeed = 5;
    public float steerSpeed = 5;
    public float acceleration = 7;
    public float randomSteerMaxDuration = 1;
    public float randomSteerStrength = 0.6f;
    public float targetSteerStrength = 3;
    public float timeBetweenDirUpdate = 0.1f;
    public float collisionAvoidSteerStrength = 5;

	

	[Header("Pheromones")]
	public float pheromoneRunOutTime = 30;
    public float dstBetweenMarkers = 2f;
    public float perceptionRadius = 5f;
    public float pheromoneEvaporateTime = 20;
    public float pheromoneWeight = 1;

	[Header("Sensing")]
    public float sensorSize = 0.75f;
    public float sensorDst = 4f;
    public float antennaDst = 2;
	

	[Header("Lifetime")]
    public float lifetime = 150;
	
}
