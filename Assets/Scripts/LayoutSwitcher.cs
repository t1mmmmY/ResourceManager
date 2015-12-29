using UnityEngine;
using System.Collections;

public class LayoutSwitcher : MonoBehaviour 
{
	[SerializeField] float limitRatio = 1.4f;

	[SerializeField] bool onUpdate = true;
	bool wideScreen = false;
	float aspectRatio = 0;

	void Start()
	{
		if (!onUpdate)
		{
			CheckAspectRatio();
		}
	}

	void FixedUpdate()
	{
		if (onUpdate)
		{
			CheckAspectRatio();
		}
	}

	void CheckAspectRatio()
	{
		float currentAspectRatio = (float)Screen.width / (float)Screen.height;

		if (currentAspectRatio != aspectRatio)
		{
			aspectRatio = currentAspectRatio;

			wideScreen = aspectRatio > limitRatio ? true : false;
		}
	}

	void OnGUI()
	{
		GUILayout.Label(aspectRatio.ToString());
		GUILayout.Label("Wide screen " + wideScreen.ToString());
	}
}
