using UnityEngine;

public class DeactivateOnStart : MonoBehaviour
{
	// Start is called before the first frame update
	private void Awake()
	{
		gameObject.SetActive(false);
	}
}