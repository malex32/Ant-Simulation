using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CounterScript : MonoBehaviour
{
    public static CounterScript instance;
    public Text counter;

    int food = 0;

    void Awake()
    {
        instance = this;
    }
    void Start()
    {
        counter.text = "Food Collected " + food.ToString();
    }

    // Update is called once per frame
    public void AddPoint()
    {
        food += 1;
        counter.text = "Food Collected " + food.ToString();
    }

}
