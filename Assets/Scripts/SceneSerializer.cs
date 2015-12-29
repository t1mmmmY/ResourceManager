using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class SceneSerializer : MonoBehaviour 
{
	[SerializeField] ObjectSerializer[] serializedObjects;

	[ContextMenu ("Find all serialized objects")]
	void FindAllSerializedObjects()
	{
		serializedObjects = FindObjectsOfType<ObjectSerializer>();
	}

	[ContextMenu ("Serialize mode A")]
	void SerializeModeA()
	{
		foreach (ObjectSerializer obj in serializedObjects)
		{
			obj.SerializeModeA();
		}
	}

	[ContextMenu ("Serialize mode B")]
	void SerializeModeB()
	{
		foreach (ObjectSerializer obj in serializedObjects)
		{
			obj.SerializeModeB();
		}
	}

	[ContextMenu ("Enable mode A")]
	void EnableModeA()
	{
		foreach (ObjectSerializer obj in serializedObjects)
		{
			obj.EnableModeA();
		}
	}

	[ContextMenu ("Enable mode B")]
	void EnableModeB()
	{
		foreach (ObjectSerializer obj in serializedObjects)
		{
			obj.EnableModeB();
		}
	}
}
