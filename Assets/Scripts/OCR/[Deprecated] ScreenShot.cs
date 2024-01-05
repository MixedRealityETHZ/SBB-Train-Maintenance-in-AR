#region

using System;
using System.Collections;
using System.IO;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

#endregion

namespace OCR
{
	[Obsolete("Class is deprecated in favor of OCRManager. Please use that instead.")]
	public class ScreenShot : MonoBehaviour
	{
		private const string APIKey = "<azure_ocr_api_key>";
		private const string Endpoint =
			"<azure_ocr_api_endpoint>vision/v3.2/read/analyze";

		public Image screenshotDisplay;
		public GameObject screenshotPanel;
		public PressableButton screenshotButton;
		
		private string _getResultUrl;
		private string _imagePath;
		private string _path;

		// Start is called before the first frame update
		private void Start()
		{
			_path = Application.temporaryCachePath + "\\";
			Debug.Log(_path);
			_imagePath = _path + "capture.png";
			screenshotPanel.SetActive(false);
		}

		// Update is called once per frame
		private void Update()
		{
		}

		public void Capture()
		{
			screenshotButton.enabled = false;
			StartCoroutine(CaptureImage());
		}

		private IEnumerator CaptureImage()
		{
			ScreenCapture.CaptureScreenshot(_imagePath);
			yield return new WaitForSeconds(1);

			var screenshotTexture = new Texture2D(2, 2);
			var imageData = File.ReadAllBytes(_imagePath);
			screenshotTexture.LoadImage(imageData);
			screenshotDisplay.sprite = Sprite.Create(screenshotTexture,
				new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), new Vector2(0.5f, 0.5f));
			screenshotPanel.SetActive(true);

			StartCoroutine(SendImageForAnalysis());
		}

		private IEnumerator SendImageForAnalysis()
		{
			var imageData = File.ReadAllBytes(_imagePath);
			var request = UnityWebRequest.Put(Endpoint, imageData);
			request.method = "POST";
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/octet-stream");
			request.SetRequestHeader("Ocp-Apim-Subscription-Key", APIKey);

			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(request.error);
			}
			else
			{
				_getResultUrl = request.GetResponseHeaders()["Operation-Location"];
				yield return new WaitForSeconds(0.1f);
				StartCoroutine(GetAnalysisResults(_getResultUrl));
			}
		}

		private IEnumerator GetAnalysisResults(string getResultUrl)
		{
			var succeeded = false;
			while (!succeeded)
			{
				var request = UnityWebRequest.Get(getResultUrl);
				request.SetRequestHeader("Ocp-Apim-Subscription-Key", APIKey);

				yield return request.SendWebRequest();

				if (request.result != UnityWebRequest.Result.Success)
				{
					Debug.Log(request.error);
				}
				else
				{
					var response = request.downloadHandler.text;
					var data = JObject.Parse(response);
					var status = data["status"].Value<string>();
					Debug.Log("Status: " + status);

					if (status == "succeeded")
					{
						succeeded = true;
						screenshotButton.enabled = true;
						screenshotPanel.SetActive(false);
						Debug.Log("Analysis succeeded.");
						Debug.Log(response);
					}
					else if (status == "running")
					{
						Debug.Log("Analysis still running... retrying in 0.5 seconds.");
						yield return new WaitForSeconds(0.5f);
					}
					else
					{
						Debug.LogError("Analysis failed or other status received.");
						yield break;
					}
				}
			}
		}
	}
}