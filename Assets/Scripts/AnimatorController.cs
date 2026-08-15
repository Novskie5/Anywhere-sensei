using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    Animator animator;
    string[] idleTriggers = {"Idle1", "Idle2", "Idle3"};
    bool isIdle = true;
    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(RandomIdleTrigger());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
