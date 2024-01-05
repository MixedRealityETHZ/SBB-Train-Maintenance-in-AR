using UnityEngine;

public class DisableInBuildMode : MonoBehaviour
{
	private void Start()
	{
		if (!Application.isEditor) gameObject.SetActive(false);
	}
}