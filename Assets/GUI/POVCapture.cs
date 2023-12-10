using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using MixedReality.Toolkit.UX;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
 
public class ScreenShot : MonoBehaviour
{
    string path;
    string image_path;
    private string endpoint = "<azure_ocr_api_endpoint>vision/v3.2/read/analyze";
    private string apiKey = "<azure_ocr_api_key>";
    private string getResultUrl;
    public UnityEngine.UI.Image screenshotDisplay; 
    public GameObject screenshotPanel;
    public GameObject labelPanel;
    //public UnityEngine.UI.Text labelText; create label
    public TMP_Text labelText;
    public PressableButton screenshotButton;

    private string patternLLNN = @"^[A-Za-z]{2}\d{2}$";
    private string patternID = @"^(?:SBB )?(\d{3}-\d{2}-\d{3})$";

    // Start is called before the first frame update
    void Start()
    { 
        path = Application.temporaryCachePath + "\\";  
        Debug.Log(path);
        image_path = path + "capture.png";
        screenshotPanel.SetActive(false);
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Capture() {
        screenshotButton.enabled = false;
        StartCoroutine(CaptureImage());
    }

    private IEnumerator CaptureImage()
    { 
        
        ScreenCapture.CaptureScreenshot(image_path);
        yield return new WaitForSeconds(1); 

        Texture2D screenshotTexture = new Texture2D(2, 2);  
        byte[] imageData = File.ReadAllBytes(image_path);
        screenshotTexture.LoadImage(imageData);
        screenshotDisplay.sprite = Sprite.Create(screenshotTexture, new Rect(0, 0, screenshotTexture.width, screenshotTexture.height), new Vector2(0.5f, 0.5f));
        screenshotPanel.SetActive(true);
        
        StartCoroutine(SendImageForAnalysis());
    }

    private IEnumerator SendImageForAnalysis()
    {
        byte[] imageData = File.ReadAllBytes(image_path);
        UnityWebRequest request = UnityWebRequest.Put(endpoint, imageData);
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
        bool succeeded = false;
        while (!succeeded) {
            UnityWebRequest request = UnityWebRequest.Get(getResultUrl);
            request.SetRequestHeader("Ocp-Apim-Subscription-Key", apiKey);

            yield return request.SendWebRequest(); 

            if (request.result != UnityWebRequest.Result.Success) 
            {
                Debug.Log(request.error);
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
                            if (matchPlaqueLabel.Success) {
                                plaqueLabel = matchPlaqueLabel.Value;
                            }
                            if (matchSBBID.Success) {
                                SBBID = matchSBBID.Groups[1].Value;
                            }
                        }
                    }


                    labelPanel.SetActive(true);
                    labelText.text = $"{plaqueLabel} - SBB {SBBID}";
            
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
