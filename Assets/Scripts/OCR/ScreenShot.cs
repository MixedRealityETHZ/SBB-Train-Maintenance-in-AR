using System.Collections;
using System.IO;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LabelScan : MonoBehaviour
{
	public Image screenshotDisplay;
	public GameObject screenshotPanel;

	public PressableButton screenshotButton;
	private readonly string apiKey = "<azure_ocr_api_key>";
	private readonly string endpoint = "<azure_ocr_api_endpoint>vision/v3.2/read/analyze";
	private string getResultUrl;
	private string image_path;
	private string path;

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

	public void Capture()
	{
		screenshotButton.enabled = false;
		StartCoroutine(CaptureImage());
	}

	private IEnumerator CaptureImage()
	{
		ScreenCapture.CaptureScreenshot(image_path);
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