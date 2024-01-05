using Microsoft.MixedReality.WebView;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class InputHandler : MonoBehaviour
{
	private IWebView webView;
	private WebView webViewComponent;

	private void Start()
	{
		webViewComponent = gameObject.GetComponent<WebView>();
	}

	public void Test(SelectEnterEventArgs args)
	{
		if (args.interactorObject is XRBaseInteractor)
		{
			var rayInteractor = args.interactorObject as XRRayInteractor;
			if (rayInteractor != null)
				webViewComponent.GetWebViewWhenReady(webView => {
					RaycastHit raycastHit;
					rayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

					var hitPointWebView = WorldToWebViewPoint(raycastHit.point, webView);

					//UnityEngine.Debug.Log("Hit point: " + hitPointWebView);
					var mouseEventsWebView = webView as IWithMouseEvents;

					var mouseEvent = new WebViewMouseEventData
					{
						X = hitPointWebView.x,
						Y = hitPointWebView.y,
						Device = WebViewMouseEventData.DeviceType.Pointer,
						Type = WebViewMouseEventData.EventType.MouseDown,
						Button = WebViewMouseEventData.MouseButton.ButtonLeft,
						TertiaryAxisDeviceType = WebViewMouseEventData.TertiaryAxisDevice.PointingDevice
					};

					mouseEventsWebView.MouseEvent(mouseEvent);

					mouseEvent.Type = WebViewMouseEventData.EventType.MouseUp;
					mouseEventsWebView.MouseEvent(mouseEvent);
				});
		}
	}

	// https://github.com/MicrosoftEdge/WebView2Feedback/issues/3681#issuecomment-1666135906
	private Vector2Int WorldToWebViewPoint(Vector3 worldPoint, IWebView webView)
	{
		// Convert the world point to our control's local space.
		var localPoint = transform.InverseTransformPoint(worldPoint);

		// Adjust the point to be based on a 0,0 origin.
		var uvTouchPoint = new Vector2(localPoint.x + 0.5f, -(localPoint.y - 0.5f));
		Debug.Log("uvTouchPoint: " + uvTouchPoint);

		return Vector2Int.RoundToInt(new Vector2(uvTouchPoint.x * webView.Width, uvTouchPoint.y * webView.Height));
	}
}