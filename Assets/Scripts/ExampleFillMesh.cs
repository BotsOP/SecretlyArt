using System.Collections;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using EventType = Managers.EventType;

public class ExampleFillMesh : MonoBehaviour
{
    [SerializeField] private float carveSpeed = 1;
    [SerializeField] private float carveSize = 1;
    private Vector3 cachedPos;
    void Update()
    {
        if (transform.position != cachedPos)
        {
            Debug.Log($"changed pos");
            EventSystem<Vector3, float, float>.RaiseEvent(EventType.FILL_TERRAIN, transform.position, transform.localScale.x / 2, carveSpeed);
        }
        cachedPos = transform.position;
    }
}
