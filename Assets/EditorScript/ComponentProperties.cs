using UnityEngine;
using System.Collections;

[System.Serializable]
public class ComponentProperties  
{
	public string[] properties;
	public int current = 0;

	public void SetProperties(string[] propertyNames)
	{
		properties = propertyNames;
	}

	public string ParamName
	{
		get { return properties[current]; }
	}
}