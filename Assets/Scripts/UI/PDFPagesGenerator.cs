#region

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

#endregion

#if WINDOWS_UWP
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Storage.Search;
#endif

namespace UI
{
	/// <summary>
	/// Loads all images in a folder and adds them as children to the attached GameObject (in alphabetical order). To
	/// be used together with UI layout groups for pagination.
	/// </summary>
	public class PDFPagesGenerator : MonoBehaviour
	{
		[Tooltip("Root folder that contains all page images. Supported formats are PNG and JPG")]
		public string pdfImageFolder;

		// Start is called before the first frame update
		private async void Start()
		{
#if WINDOWS_UWP
		string commonPath = KnownFolders.Objects3D.Path;
#else
			var commonPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
#endif
			var folderPath = commonPath + "\\" + pdfImageFolder;
			var imagePaths = Directory.GetFiles(folderPath);

			foreach (var imagePath in imagePaths)
			{
				if (!(imagePath.EndsWith(".png") || imagePath.EndsWith(".jpg"))) continue;

				var rawImage = new GameObject().AddComponent<RawImage>();
				rawImage.name = imagePath;
				Transform t = rawImage.transform;
				t.SetParent(gameObject.transform);
				t.localPosition = new Vector3();
				t.localScale = new Vector3(1f, 1f, 1f);
				var texture = new Texture2D(1, 1);
				texture.LoadImage(File.ReadAllBytes(imagePath));
				rawImage.texture = texture;
				var parentWidth = transform.parent.GetComponent<RectTransform>().rect.width;
				rawImage.rectTransform.anchorMax = rawImage.rectTransform.anchorMin;
				rawImage.rectTransform.sizeDelta = new Vector2(parentWidth, parentWidth * texture.height / texture.width);
			}
		}
	}
}