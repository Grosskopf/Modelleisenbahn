﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class lookaway : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform camera;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = Quaternion.LookRotation(transform.position - camera.position);
    }
}
