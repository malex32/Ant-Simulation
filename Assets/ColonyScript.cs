using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Colony : MonoBehaviour
{
    public GameObject FoodPrefab;
    private Camera mainCamera;

    public PerceptionMap globalHomeMarkers;
    public PerceptionMap globalFoodMarkers;
    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SpawnFood();
        }

    }

    void SpawnFood()
    {
        Vector3 mousePosition = Input.mousePosition;

        // Convert the mouse position to world coordinates
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

        // Ensure the z-coordinate is set to 0 (or the appropriate z-coordinate for your 2D setup)
        worldPosition.z = 0f;

        // Instantiate the Food prefab at the world position
        Instantiate(FoodPrefab, worldPosition, Quaternion.identity);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PickedUpFood"))
        {
            CounterScript.instance.AddPoint();
        }
    }
}
