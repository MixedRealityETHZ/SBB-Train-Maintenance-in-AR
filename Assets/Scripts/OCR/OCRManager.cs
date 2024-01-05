#region

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json.Linq;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

#endregion

namespace OCR
{
	/// <summary>
	///     Manages label scanning, screenshot display and other related tasks.
	/// </summary>
	public class OCRManager : MonoBehaviour
	{
		private const string Endpoint = "<azure_ocr_api_endpoint>vision/v3.2/read/analyze";
		private const string APIKey = "<azure_ocr_api_key>";
		private const string PatternID = @"^(?:SBB )?(\d{3}-\d{2}-\d{3})$";
		private const string PatternLN = @"^[A-Za-z]{2}\d{2}$";

		[Tooltip("Raw Image UI element displaying the screenshot")]
		public RawImage screenshotDisplay;

		[Tooltip("Panel containing screenshotDisplay. Used to activate/deactivate screenshot.")]
		public GameObject screenshotPanel;

		[Tooltip("Checklist generator script. Used to sync checklist with scanned label.")]
		public ChecklistGenerator checklistGenerator;

		[Tooltip("GameObject containing the notification text. Has to have a TMP_Text in its children.")]
		public GameObject statusTextPlate;

		[Tooltip("Text used to display the scanned label")]
		public TMP_Text labelText;

		[Tooltip("GameObject containing scanned label text")]
		public GameObject labelPanel;

		[Tooltip("Reference to hand menu to get open/closed state")]
		public HandConstraintPalmUp handMenu;

		[Tooltip("Reference to 'Take screenshot' button")]
		public PressableButton screenshotButton;

		[Tooltip(
			"Toggle recording mode. If on, a mock screenshot will be used instead of taking an actual screenshot.")]
		public bool enableRecordingMode;

		[FormerlySerializedAs("mockImage")]
		[Tooltip("Mock screenshots to use when recording mode is enabled. They are cycled in order.")]
		public Texture2D[] mockImages;

		private string _getResultUrl;
		private Texture _image;
		private string _imagePath;
		private int _mockImageIndex = -1; // Start at -1 so that incrementing the first time returns 0
		private string _path;
		private PhotoCapture _photoCaptureObject;
		private Coroutine _statusTimer;

		// Start is called before the first frame update
		private void Start()
		{
			_path = Application.temporaryCachePath + "/";
			Debug.Log(_path);
			_imagePath = _path + "capture.png";
			screenshotPanel.SetActive(false);
		}

		private void SetStatusText(string text)
		{
			statusTextPlate.SetActive(true);
			statusTextPlate.GetComponentInChildren<TMP_Text>().text = text;
		}

		private void HideStatusText()
		{
			statusTextPlate.SetActive(false);
		}

		private IEnumerator SetTimedStatusTextCoroutine(string text, float duration)
		{
			SetStatusText(text);
			yield return new WaitForSeconds(duration);
			HideStatusText();
			_statusTimer = null;
		}

		private void SetTimedStatusText(string text, float duration)
		{
			if (_statusTimer != null) StopCoroutine(_statusTimer);

			StartCoroutine(SetTimedStatusTextCoroutine(text, duration));
		}

		private void OnPhotoCaptureCreated(PhotoCapture captureObject)
		{
			Debug.Log("Photo Capture Object Created");
			_photoCaptureObject = captureObject;

			var cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending(res => res.width * res.height)
				.First();

			var c = new CameraParameters();
			c.hologramOpacity = 0.0f;
			c.cameraResolutionWidth = cameraResolution.width;
			c.cameraResolutionHeight = cameraResolution.height;
			c.pixelFormat = CapturePixelFormat.BGRA32;

			captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
		}

		private void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
		{
			_photoCaptureObject.Dispose();
			_photoCaptureObject = null;
		}

		private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
		{
			if (result.success)
			{
				_photoCaptureObject.TakePhotoAsync(_imagePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
			}
			else
			{
				Debug.LogError("Unable to start photo mode!");
				SetTimedStatusText("Error starting photo mode :(", 4.0f);
			}
		}

		private void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
		{
			if (result.success)
			{
				Debug.Log("Saved Photo to disk!");
				HideStatusText();
				ShowHUD();
				_photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
			}
			else
			{
				Debug.Log("Failed to save Photo to disk");
				SetTimedStatusText("Error saving photo capture :(", 4.0f);
			}
		}

		/// <summary>
		///     Starts the appropriate screenshot capture procedure. This performs different actions depending on the
		///     context:
		/// 
		///     - In the Unity editor, it uses the Unity API for capturing a screenshot and saves it to a file
		///     - In UWP build, uses the HL's webcam capabilities to take a screenshot and save it to a file
		///     - If `enableRecordingMode` is enabled, no screenshot is taken and instead the textures saved in
		///		  `mockImages` are used
		/// 
		///     In any case, a photo is sent to an Azure AI Vision endpoint which analyzes the image and returns all the
		///     recognized text. All information is shown as UI elements to the user.
		/// </summary>
		public void Capture()
		{
			screenshotButton.enabled = false;

			// Recording mode takes preference over everything
			if (enableRecordingMode)
			{
				// "Advance" image index by one and wrap around (starts at -1, so first time will be 0)
				_mockImageIndex = (_mockImageIndex + 1) % mockImages.Length;
				ShowHUD(ImageSource.MockImage);
			}
			else if (Application.isEditor)
			{
				ScreenCapture.CaptureScreenshot(_imagePath);
				ShowHUD();
			}
			else
			{
				SetStatusText("Hold still...");
				PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
			}
		}

		private void ShowHUD(ImageSource imageSource = ImageSource.Path)
		{
			screenshotPanel.SetActive(true);

			Texture2D screenshotTexture;
			switch (imageSource)
			{
				case ImageSource.Path:
					screenshotTexture = new Texture2D(2, 2);
					var imageData = File.ReadAllBytes(_imagePath);
					screenshotTexture.LoadImage(imageData);
					break;
				case ImageSource.MockImage:
					screenshotTexture = mockImages[_mockImageIndex];
					break;
				default:
					throw new ArgumentException("Please provide an image source");
			}

			screenshotDisplay.texture = screenshotTexture;

			StartCoroutine(SendImageForAnalysis(imageSource));
		}

		private IEnumerator SendImageForAnalysis(ImageSource imageSource = ImageSource.Path)
		{
			Debug.Log("Sending OCR request");

			var imageData = imageSource switch
			{
				ImageSource.Path => File.ReadAllBytes(_imagePath),
				ImageSource.MockImage => mockImages[_mockImageIndex].EncodeToPNG(),
				_ => throw new ArgumentException("Please provide an image source")
			};

			var uploadHandler = new UploadHandlerRaw(imageData);
			var downloadHandler = new DownloadHandlerBuffer();
			var request = new UnityWebRequest(Endpoint, "POST", downloadHandler, uploadHandler);
			request.SetRequestHeader("Content-Type", "application/octet-stream");
			request.SetRequestHeader("Ocp-Apim-Subscription-Key", APIKey);

			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError("SendingImageForAnalysis: " + FormatErrorResponse(request));
				screenshotButton.enabled = true;
				screenshotPanel.SetActive(false);
			}
			else
			{
				_getResultUrl = request.GetResponseHeaders()["Operation-Location"];
				StartCoroutine(GetAnalysisResults(_getResultUrl));
			}
		}

		private string FormatErrorResponse(UnityWebRequest request)
		{
			var formattedHeaders =
				string.Join("\n", request.GetResponseHeaders().Select(kv => $"{kv.Key}: {kv.Value}"));
			return
				$"ERROR {request.responseCode}: {request.error}.\nResponse body:\n{request.result}\nHeaders:\n{formattedHeaders}";
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
					Debug.LogError("GetAnalysisResults: " + FormatErrorResponse(request));
					screenshotButton.enabled = true;
					screenshotPanel.SetActive(false);
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
						var plaqueLabel = "n/a";
						var SBBID = "n/a";

						var readResults = data["analyzeResult"]["readResults"];
						foreach (var readResult in readResults)
						{
							var lines = readResult["lines"];
							foreach (var line in lines)
							{
								var text = line["text"].Value<string>();
								Debug.Log("Extracted Text: " + text);
								var matchPlaqueLabel = Regex.Match(text, PatternLN);
								var matchSBBID = Regex.Match(text, PatternID);
								if (matchPlaqueLabel.Success) plaqueLabel = matchPlaqueLabel.Value;

								if (matchSBBID.Success) SBBID = matchSBBID.Groups[1].Value;
							}
						}


						if (plaqueLabel == "n/a" || SBBID == "n/a")
						{
							SetTimedStatusText("Failed to detect label", 4.0f);
						}
						else
						{
							// Label found
							labelText.text = $"{plaqueLabel} - SBB {SBBID}";
							checklistGenerator.SetDoor(plaqueLabel);
							SetTimedStatusText($"Found label: {plaqueLabel}", 4.0f);

							// Only enable label panel if hand menu is open
							if (handMenu.Handedness != Handedness.None) labelPanel.SetActive(true);
						}
					}
					else if (status == "running")
					{
						Debug.Log("Analysis still running... retrying in 1 second.");
						yield return new WaitForSeconds(1.0f);
						SetStatusText("Still analyzing... Hold tight");
					}
					else
					{
						Debug.LogError("ERROR: Analysis failed or other status received.");
						screenshotButton.enabled = true;
						screenshotPanel.SetActive(false);
						yield break;
					}
				}
			}
		}
	}

	internal enum ImageSource
	{
		Path = 0,
		MockImage = 1
	}
}