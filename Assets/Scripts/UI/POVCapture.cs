using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Linq;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Windows.WebCam;

public class ScreenShot : MonoBehaviour
{
	string path;
	string image_path;
	private string endpoint = "<azure_ocr_api_endpoint>vision/v3.2/read/analyze";
	private string apiKey = "<azure_ocr_api_key>";
	private string getResultUrl;
	public RawImage screenshotDisplay;
	public GameObject screenshotPanel;
	public ChecklistGenerator checklistGenerator;

	public GameObject labelPanel;

	//public UnityEngine.UI.Text labelText; create label
	public TMP_Text labelText;
	public PressableButton screenshotButton;

	private string patternLLNN = @"^[A-Za-z]{2}\d{2}$";
	private string patternID = @"^(?:SBB )?(\d{3}-\d{2}-\d{3})$";

	private PhotoCapture photoCaptureObject = null;
	private Texture image;

	// Start is called before the first frame update
	void Start()
	{
		path = Application.temporaryCachePath + "/";
		Debug.Log(path);
		image_path = path + "capture.png";
		screenshotPanel.SetActive(false);
	}

	// Update is called once per frame
	void Update()
	{
	}

	void OnPhotoCaptureCreated(PhotoCapture captureObject)
	{
		UnityEngine.Debug.Log("Photo Capture Object Created");
		photoCaptureObject = captureObject;

		Resolution cameraResolution =
			PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();

		CameraParameters c = new CameraParameters();
		c.hologramOpacity = 0.0f;
		c.cameraResolutionWidth = cameraResolution.width;
		c.cameraResolutionHeight = cameraResolution.height;
		c.pixelFormat = CapturePixelFormat.BGRA32;

		captureObject.StartPhotoModeAsync(c, OnPhotoModeStarted);
	}

	void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
	{
		photoCaptureObject.Dispose();
		photoCaptureObject = null;
	}

	private void OnPhotoModeStarted(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			photoCaptureObject.TakePhotoAsync(image_path, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
		}
		else
		{
			Debug.LogError("Unable to start photo mode!");
		}
	}

	void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
	{
		if (result.success)
		{
			Debug.Log("Saved Photo to disk!");
			ShowHUD();
			photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
		}
		else
		{
			Debug.Log("Failed to save Photo to disk");
		}
	}

	public void Capture()
	{
		screenshotButton.enabled = false;

		if (Application.isEditor)
		{
			screenshotPanel.SetActive(false);
			labelPanel.SetActive(false);
			ScreenCapture.CaptureScreenshot(image_path);

			ShowHUD();
		}
		else
		{
			PhotoCapture.CreateAsync(false, OnPhotoCaptureCreated);
		}
	}

	private void ShowHUD()
	{
		Debug.Log("Showing HUD");
		Texture2D screenshotTexture = new Texture2D(2, 2);
		byte[] imageData = File.ReadAllBytes(image_path);
		screenshotTexture.LoadImage(imageData);
		screenshotDisplay.texture = screenshotTexture;
		screenshotPanel.SetActive(true);

		StartCoroutine(SendImageForAnalysis());
	}

	private IEnumerator SendImageForAnalysis()
	{
		Debug.Log("Sending OCR request");
		byte[] imageData = File.ReadAllBytes(image_path);
		var uploadHandler = new UploadHandlerRaw(imageData);
		var downloadHandler = new DownloadHandlerBuffer();
		UnityWebRequest request = new UnityWebRequest(endpoint, "POST", downloadHandler, uploadHandler);
		request.SetRequestHeader("Content-Type", "application/octet-stream");
		request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

		yield return request.SendWebRequest();

		if (request.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError("SendingImageForAnalysis: " + FormatErrorResponse(request));
			screenshotButton.enabled = true;
			screenshotPanel.SetActive(false);
		}
		else
		{
			getResultUrl = request.GetResponseHeaders()["Operation-Location"];
			StartCoroutine(GetAnalysisResults(getResultUrl));
		}
	}

	private string FormatErrorResponse(UnityWebRequest request)
	{
		string formattedHeaders = string.Join("\n", request.GetResponseHeaders().Select(kv => $"{kv.Key}: {kv.Value}"));
		return
			$"ERROR {request.responseCode}: {request.error}.\nResponse body:\n{request.result}\nHeaders:\n{formattedHeaders}";
	}

	private IEnumerator GetAnalysisResults(string getResultUrl)
	{
		bool succeeded = false;
		while (!succeeded)
		{
			UnityWebRequest request = UnityWebRequest.Get(getResultUrl);
			request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);
			yield return request.SendWebRequest();

			if (request.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError("GetAnalysisResults: " + FormatErrorResponse(request));
				screenshotButton.enabled = true;
				screenshotPanel.SetActive(false);
			}
			else
			{
				string response = request.downloadHandler.text;
				var data = JObject.Parse(response);
				string status = data["status"].Value<string>();
				Debug.Log("Status: " + status);

				if (status == "succeeded")
				{
					succeeded = true;
					screenshotButton.enabled = true;
					screenshotPanel.SetActive(false);
					Debug.Log("Analysis succeeded.");
					Debug.Log(response);
					string plaqueLabel = "n/a";
					string SBBID = "n/a";

					var readResults = data["analyzeResult"]["readResults"];
					foreach (var readResult in readResults)
					{
						var lines = readResult["lines"];
						foreach (var line in lines)
						{
							string text = line["text"].Value<string>();
							Debug.Log("Extracted Text: " + text);
							Match matchPlaqueLabel = Regex.Match(text, patternLLNN);
							Match matchSBBID = Regex.Match(text, patternID);
							if (matchPlaqueLabel.Success)
							{
								plaqueLabel = matchPlaqueLabel.Value;
							}

							if (matchSBBID.Success)
							{
								SBBID = matchSBBID.Groups[1].Value;
							}
						}
					}


					labelPanel.SetActive(true);
					labelText.text = $"{plaqueLabel} - SBB {SBBID}";
					checklistGenerator.SetDoor(plaqueLabel);
				}
				else if (status == "running")
				{
					Debug.Log("Analysis still running... retrying in 0.5 seconds.");
					yield return new WaitForSeconds(1.0f);
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
