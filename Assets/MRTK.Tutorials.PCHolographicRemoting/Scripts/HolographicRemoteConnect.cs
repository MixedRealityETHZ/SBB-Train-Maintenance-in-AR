// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.MixedReality.OpenXR.Remoting;
using UnityEngine;

public class HolographicRemoteConnect : MonoBehaviour
{
	[SerializeField] private string IP;

	[SerializeField] [Tooltip("The configuration information for the remote connection.")]
	private RemotingConnectConfiguration remotingConfiguration = new() { RemotePort = 8265, MaxBitrateKbps = 20000 };

	private bool connected;

	private void OnGUI()
	{
		IP = GUI.TextField(new Rect(10, 10, 200, 30), IP, 25);

		var buttonText = connected ? "Disconnect" : "Connect";

		if (GUI.Button(new Rect(220, 10, 100, 30), buttonText))
		{
			if (connected)
			{
				AppRemoting.Disconnect();
				connected = false;
			}
			else
			{
				Connect();
			}

			Debug.Log(buttonText);
		}
	}

	public void Connect()
	{
		connected = true;

		remotingConfiguration.RemoteHostName = IP;

		AppRemoting.StartConnectingToPlayer(remotingConfiguration);
	}
}