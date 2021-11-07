using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThinkMover : MonoBehaviour
{
    public ThinkGearConnector connector;

    public float attentionSmooth = 0;
    public float smooth = 0.99f;

    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        attentionSmooth = connector.attention * (1 - smooth) + attentionSmooth * smooth;

        transform.position = new Vector3(0, attentionSmooth * 0.1f, 0);
    }
}
