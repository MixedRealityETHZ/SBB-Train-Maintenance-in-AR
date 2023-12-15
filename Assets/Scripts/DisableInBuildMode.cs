using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableInBuildMode : MonoBehaviour
{
    private void Start()
    {
        if (!Application.isEditor)
        {
            gameObject.SetActive(false);
        }
    }
}
