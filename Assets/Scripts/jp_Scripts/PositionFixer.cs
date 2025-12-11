using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionFixer : MonoBehaviour
{
    [Tooltip("Please don't rotate any of its parents")]
    public Vector3 OriginalPosition;

    void Start()
    {
        
    }

    void Update()
    {
        transform.position = OriginalPosition;
    }
}
