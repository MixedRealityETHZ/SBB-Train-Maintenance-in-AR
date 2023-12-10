using UnityEngine;

using Microsoft.MixedReality.WebView;
using UnityEngine.XR.Interaction.Toolkit;

public class InputHandler : MonoBehaviour
{
    private WebView webViewComponent;
    private IWebView webView;

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
            {
                webViewComponent.GetWebViewWhenReady((IWebView webView) =>
                {
                    RaycastHit raycastHit;
                    rayInteractor.TryGetCurrent3DRaycastHit(out raycastHit);

                    Vector2Int hitPointWebView = WorldToWebViewPoint(raycastHit.point, webView);
                    //UnityEngine.Debug.Log("Hit point: " + hitPointWebView);

                    IWithMouseEvents mouseEventsWebView = webView as IWithMouseEvents;

                    WebViewMouseEventData mouseEvent = new WebViewMouseEventData
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

    }

    // https://github.com/MicrosoftEdge/WebView2Feedback/issues/3681#issuecomment-1666135906
    private Vector2Int WorldToWebViewPoint(Vector3 worldPoint, IWebView webView)
    {
        // Convert the world point to our control's local space.
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // Adjust the point to be based on a 0,0 origin.
        var uvTouchPoint = new Vector2((localPoint.x + 0.5f), -(localPoint.y - 0.5f));
        UnityEngine.Debug.Log("uvTouchPoint: " + uvTouchPoint);

        return Vector2Int.RoundToInt(new Vector2(uvTouchPoint.x * webView.Width, uvTouchPoint.y * webView.Height));
    }
}