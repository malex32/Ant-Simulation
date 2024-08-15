using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

public class PerceptionMap : MonoBehaviour
{
    // Other Scripts
    public AntSettings antSettings;

    public Vector2 area;
    int numCellsX;
	int numCellsY;
    Vector2 halfSize;
	float cellSizeReciprocal;
	Cell[, ] cells;
    float sqrPerceptionRadius;
    float perceptionRadius;

    void Awake()
    {
        Init();
    }


    void Init()
    {
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        area = new Vector2(screenWidth, screenHeight);
        perceptionRadius = Mathf.Max (0.01f, antSettings.sensorSize);
        sqrPerceptionRadius = perceptionRadius * perceptionRadius; // not used yet
        numCellsX = Mathf.CeilToInt (area.x / perceptionRadius);
		numCellsY = Mathf.CeilToInt (area.y / perceptionRadius);
        halfSize = new Vector2 (numCellsX * perceptionRadius, numCellsY * perceptionRadius) * 0.5f;
		cellSizeReciprocal = 1 / perceptionRadius;
        cells = new Cell[numCellsX, numCellsY];

		for (int y = 0; y < numCellsY; y++) 
        {
			for (int x = 0; x < numCellsX; x++) 
            {
				cells[x, y] = new Cell ();
			}
		}
    }

    void Update()
    {
        float currentTime = Time.time;

        for (int y = 0; y < numCellsY; y++) 
        {
            for (int x = 0; x < numCellsX; x++) 
            {
                Cell cell = cells[x, y];
                var currentEntryNode = cell.entries.First;

                while (currentEntryNode != null)
                {
                    Entry currentEntry = currentEntryNode.Value;
                    float currentLifetime = currentTime - currentEntry.creationTime;

                    // Adjust color based on lifetime or other conditions
                    if (currentEntry.pheromoneObject != null)
                    {
                        Renderer renderer = currentEntry.pheromoneObject.GetComponent<Renderer>();
                        Color color = renderer.material.color;
                        // Make the color more transparent over time
                        color.a = Mathf.Max(0, 1 - currentLifetime / antSettings.pheromoneEvaporateTime);
                        renderer.material.color = color;
                    }

                    currentEntryNode = currentEntryNode.Next;
                }
            }
        }
    }
    public void Add (Vector2 point, float initialWeight, GameObject pheromone) 
    {
		Vector2Int cellCoord = CellCoordFromPos (point);
		Cell cell = cells[cellCoord.x, cellCoord.y];
		Entry entry = new Entry () { position = point, creationTime = Time.time, initialWeight = initialWeight, pheromoneObject = pheromone};
		cell.Add (entry);
    }

    public int GetAllInCircle(Entry[] result, Vector2 center)
    {
        Vector2Int cellCord = CellCoordFromPos (center);
        int i = 0;
        float currentTime = Time.time;

        for (int offSetY = -1; offSetY <= 1; offSetY++)
        {
            for (int offSetX = -1; offSetX <= 1; offSetX++)
            {
                int cellX = cellCord.x + offSetX;
                int cellY = cellCord.y + offSetY;
                if (cellX >= 0 && cellX < numCellsX && cellY >= 0 && cellY < numCellsY)
                {
                    Cell cell = cells[cellX, cellY];

                    var currentEntryNode = cell.entries.First;
                    while (currentEntryNode != null)
                    {
                        Entry currentEntry = currentEntryNode.Value;
                        float currentLifetime = currentTime - currentEntry.creationTime;

                        if (currentLifetime > antSettings.pheromoneEvaporateTime)
                        {
                            if (currentEntry.pheromoneObject != null)
                            {
                                Destroy(currentEntry.pheromoneObject); // Destroy the associated GameObject
                            }
                            cell.entries.Remove(currentEntryNode);
                        }

                        else if ((currentEntry.position - center).sqrMagnitude < sqrPerceptionRadius)
                        {
                            if (i >= result.Length)
                            {
                                return result.Length;
                            }
                            result[i] = currentEntry;
                            i++;
                        }
                        currentEntryNode = currentEntryNode.Next;
                    }
                }

            }
        }
        return i;
    }

    Vector2Int CellCoordFromPos (Vector2 point) 
    {
		int x = (int) ((point.x + halfSize.x) * cellSizeReciprocal);
		int y = (int) ((point.y + halfSize.y) * cellSizeReciprocal);
		return new Vector2Int (Mathf.Clamp (x, 0, numCellsX - 1), Mathf.Clamp (y, 0, numCellsY - 1));
	}

    public class Cell 
    {
		public LinkedList<Entry> entries;

		public Cell () {
			entries = new LinkedList<Entry> ();
		}

		public void Add (Entry entry) {
			entries.AddLast (entry);

		}

	}

    public struct Entry 
    {
		public Vector2 position;
		public float initialWeight;
		public float creationTime;
        public GameObject pheromoneObject;
	}

}