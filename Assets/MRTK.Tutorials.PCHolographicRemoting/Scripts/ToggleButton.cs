// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region

using UnityEngine;

#endregion

public class ToggleButton : MonoBehaviour
{
	[SerializeField] private GameObject ClippingObject;

	public void ToggleClipping()
	{
		ClippingObject.SetActive(!ClippingObject.activeInHierarchy);
	}
}