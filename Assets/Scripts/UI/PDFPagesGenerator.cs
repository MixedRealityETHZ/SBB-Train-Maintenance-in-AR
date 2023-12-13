using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.GraphicsTools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.OSX;
using UnityEngine.UI;
using System.IO;
using UnityEditor;

public class PDFPagesGenerator : MonoBehaviour
{
    public string PDFImageFolder;

    // Start is called before the first frame update
    void Start()
    {
        string folderPath = PDFImageFolder;
        string[] imagePaths = Directory.GetFiles(folderPath);

        foreach (var imagePath in imagePaths)
        {
            if (!(imagePath.EndsWith(".png") || imagePath.EndsWith(".jpg")))
            {
                continue;
            }

            RawImage rawImage = new GameObject().AddComponent<RawImage>();
            rawImage.name = imagePath;
            rawImage.transform.SetParent(gameObject.transform);
            rawImage.transform.localPosition = new Vector3();
            rawImage.transform.localScale = new Vector3(1f, 1f, 1f);
            Texture2D texture = new Texture2D(1, 1);
            texture.LoadImage(File.ReadAllBytes(imagePath));
            rawImage.texture = texture;
            var parentWidth = transform.parent.GetComponent<RectTransform>().rect.width;
            rawImage.rectTransform.anchorMax = rawImage.rectTransform.anchorMin;
            rawImage.rectTransform.sizeDelta = new Vector2(parentWidth, parentWidth * (float)texture.height/(float)texture.width);
        }
    }
}
