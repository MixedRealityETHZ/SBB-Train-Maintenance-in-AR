#region

using System;
using System.Collections;
using System.IO;
using System.Linq;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

#endregion

namespace OCR
{
	[Obsolete("Deprecated in favor of OCRManager. Please use that instead.")]
	public class LabelScanner : MonoBehaviour
	{
		public Image screenshotDisplay;
		public GameObject screenshotPanel;

		public PressableButton screenshotButton;
		private readonly string apiKey = "<azure_ocr_api_key>";
		private readonly string endpoint = "<azure_ocr_api_endpoint>vision/v3.2/read/analyze";
		private string getResultUrl;
		private string image_path;
		private string path;

		private PhotoCapture photoCaptureObject;

		// Start is called before the first frame update
		private void Start()
		{
			path = Application.temporaryCachePath + "\\";
			Debug.Log(path);
			image_path = path + "capture.png";
			screenshotPanel.SetActive(false);
		}

		// Update is called once per frame
		private void Update()
		{
		}

		private void OnPhotoCaptureCreated(PhotoCapture captureObject)
		{
			Debug.Log("Photo Capture Object Created");
			photoCaptureObject = captureObject;

			var cameraResolution =
				PhotoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height).First();

			var c = new CameraParameters();
			c.hologramOpacity = 0.0f;
			c.cameraResolutionWidth = cameraResolution.width;
			c.cameraResolutionHeight = cameraResolution.height;
			c.pixelFormat = CapturePixelFormat.BGRA32;

			captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
		}

		private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
		{
			photoCaptureObject.Dispose();
			photoCaptureObject = null;
		}

		private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
		{
			if (result.success)
				photoCaptureObject.TakePhotoAsync(image_path, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
			else
				Debug.LogError("Unable to start photo mode!");
		}

		private void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
		{
			if (result.success)
			{
				Debug.Log("Saved Photo to disk!");
				photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
			}
			else
			{
				Debug.Log("Failed to save Photo to disk");
			}
		}

		public void Capture()
		{
			StartCoroutine(CaptureImage());
			screenshotButton.enabled = false;
		}

		private IEnumerator CaptureImage()
		{
			if (Application.isEditor)
				ScreenCapture.CaptureScreenshot(image_path);
			else
				PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
			yield return new WaitForSeconds(1);

			var screenshotTexture = new Texture2D(2, 2);
			var imageData = File.ReadAllBytes(image_path);
			screenshotTexture.LoadImage(imageData);
			screenshotDisplay.sprite = Sprite.Create(screenshotTexture,
				new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), new Vector2(0.5f, 0.5f));
			screenshotPanel.SetActive(true);

			StartCoroutine(SendImageForAnalysis());
		}

		private IEnumerator SendImageForAnalysis()
		{
			var imageData = File.ReadAllBytes(image_path);
			var request = UnityWebRequest.Put(endpoint, imageData);
			request.method = "POST";
			request.downloadHandler = new DownloadHandlerBuffer();
			request.SetRequestHeader("Content-Type", "application/octet-stream");
			request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.Log(request.error);
			}
			else
			{
				getResultUrl = request.GetResponseHeaders()["Operation-Location"];
				yield return new WaitForSeconds(0.1f);
				StartCoroutine(GetAnalysisResults(getResultUrl));
			}
		}

		private IEnumerator GetAnalysisResults(string getResultUrl)
		{
			var succeeded = false;
			while (!succeeded)
			{
				var request = UnityWebRequest.Get(getResultUrl);
				request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

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