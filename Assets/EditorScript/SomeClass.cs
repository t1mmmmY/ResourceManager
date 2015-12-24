using UnityEngine;
using System.Collections;

[System.Serializable]
public class ComponentProperties  
{
	public string[] properties = {"position", "color", "anything"};
	public int current = 0;
}

public class SomeClass : MonoBehaviour 
{
	[SerializeField] ComponentProperties potionResult;
	public string otherStuff;
}