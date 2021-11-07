using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public ThinkGearConnector connector;
    public bool isEating;
    public float walkingSpeed;
    Animator m_Animator;

    void Start()
    {
        m_Animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        m_Animator.SetBool("Eat_b", isEating);
        m_Animator.SetFloat("Speed_f", walkingSpeed);

        if (connector.attention > 50)
        {
            m_Animator.SetBool("Eat_b", true);
        }
    }
}
