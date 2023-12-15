using System;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Search;
#endif

public class PDFPagesGenerator : MonoBehaviour
{
    public string PDFImageFolder;

    // Start is called before the first frame update
    async void Start()
    {
#if WINDOWS_UWP
        string commonPath = KnownFolders.Objects3D.Path;
#else
        string commonPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
#endif
        string folderPath = commonPath + "\\" + PDFImageFolder;
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
