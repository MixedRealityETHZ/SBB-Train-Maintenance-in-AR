using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.GraphicsTools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.OSX;
using UnityEngine.UI;

public class PDFPagesGenerator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        int num_pages = 10; for (int i = 0; i < num_pages; i++)
        {
            GameObject newPage = new GameObject(); RawImage rawImage = newPage.AddComponent<RawImage>();
            rawImage.transform.SetParent(gameObject.transform);
            rawImage.transform.localPosition = new Vector3();
            rawImage.transform.localScale = new Vector3(1f, 1f, 1f);
        }
    }
}
