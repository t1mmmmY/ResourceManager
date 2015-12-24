using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

[Serializable]
public class TargetParameters
{
	public Component Component;
	public String ParamName;
}

[Serializable]
public class Parameter
{
	public string ComponentName;
	public string ParamName;
	public string ParamType;
	public string Value;
}

[Serializable]
public class ModeParameters
{
	public string GUID;
	public string ModeName;
	public List<Parameter> Parameters;
}

// Class used to serialize all modes in 1 JSON
[Serializable]
public class ObjectParameters
{
	public string Name;
	public string GUID;
	public ModeParameters modeParameters;
}

// We need this class to be able to serialize and store UnityEngine related data in UnityEditor
[Serializable]
public class CachedParameter
{
	public string Key;
	public string ModeGUID;
	public UnityEngine.Object Value;

	public CachedParameter(string key, string modeGuid, UnityEngine.Object value)
	{
		Key = key;
		ModeGUID = modeGuid;
		Value = value;
	}
}
	
[ExecuteInEditMode]
public class ObjectSerializer : MonoBehaviour
{
	public string GUID;
	public TargetParameters[] trackableParameters;

	[SerializeField]
	private List<CachedParameter> cachedUnityObjects;

	// Temporary visible fields
	public bool _____________;
	[SerializeField]
	private string modeAGUID;
	[SerializeField]
	private string modeBGUID;
	[SerializeField]
	private string serializedModeA;
	[SerializeField]
	private string serializedModeB;

	void Start()
	{
		if (string.IsNullOrEmpty(GUID))
		{
			GUID = Guid.NewGuid().ToString();
			this.modeAGUID = Guid.NewGuid().ToString();
			this.modeBGUID = Guid.NewGuid().ToString();
		}
	}
		
	[ContextMenu ("Inspect Object")]
	void InspectObject()
	{
		foreach (var component in gameObject.GetComponents(typeof(Component)))
		{
			Type type = component.GetType();
			Debug.Log(">> " + type);

			foreach (var f in type.GetFields().Where(f => f.IsPublic))
			{
				Debug.Log("Field: " + f.Name + " : " + f.FieldType + "; " + f.GetValue(component));
			}

			var ignoredTypes = new List<Type>();
			ignoredTypes.Add(typeof(UnityEngine.Rigidbody));

			foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(i => !ignoredTypes.Contains(i.PropertyType)))
			{
				Debug.Log("Property: " + p.Name + " : " + p.PropertyType);// + "; " + p.GetValue(component, null));
			}
		}
	}

	/// <summary>
	/// Serializes the current mode.
	/// </summary>
	/// <returns>Serialized json</returns>
	/// <param name="modeName">Mode name.</param>
	/// <param name="modeGUID">Mode GUI. Used to map objects in JSON with Objects in cachedUnityObjects list on component</param>
	private string SerializeCurrentMode(string modeName, string modeGUID)
	{
		var modeParams = new ModeParameters();
		modeParams.GUID = modeGUID;
		modeParams.ModeName = modeName;
		modeParams.Parameters = new List<Parameter>();

		// Build list that contains Components (only necessary information)
		var trackComponentsList = this.trackableParameters.Select(i => i.Component);
		// Build list of gameObject's components that present in prev list
		var trackComponents = gameObject.GetComponents(typeof(Component)).Where(c => trackComponentsList.Contains(c));
		// Go through particular gameObject's components only
		foreach (var component in trackComponents)
		{
			// Get type of component
			Type type = component.GetType();
			// Build list of required parameter names. To avoid reading all the properties
			var parameterList = this.trackableParameters.Where(i => i.Component == component).Select(i => i.ParamName);
			// Read only required parameters of gameObject's component
			foreach (var f in type.GetFields().Where(i => i.IsPublic && parameterList.Contains(i.Name)))
			{
				//TODO Check if type implements IConvertible interface
				// Simple types derived from struct and string serialize as is
				if (f.FieldType.IsValueType || f.FieldType == typeof(String))
				{
					Parameter param = new Parameter();
					param.ComponentName = type.ToString();
					param.ParamName = f.Name;
					param.ParamType = f.FieldType.ToString();
					param.Value = f.GetValue(component).ToString();
						
					modeParams.Parameters.Add(param);
				}
				// UnityEngine objects save in cachedUnityObjects list which is serialized by Unity in UnityEditor
				if (f.FieldType.IsSubclassOf(typeof(UnityEngine.Object)))
				{
					Parameter param = new Parameter();
					param.ComponentName = type.ToString();
					param.ParamName = f.Name;
					param.ParamType = "CachedUnityObject";
					param.Value = type + "." + f.Name;

					// Remove old parameter for this mode
					this.cachedUnityObjects.RemoveAll(i => i.Key == param.Value && i.ModeGUID == modeGUID);
					// Add new parameter for this mode
					this.cachedUnityObjects.Add(new CachedParameter(param.Value, 
																	modeGUID,
																	(UnityEngine.Object)f.GetValue(component)));
					modeParams.Parameters.Add(param);
				}
			}
			//TODO Implment processing Properties
//			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
//			foreach (var p in type.GetProperties(flags).Where(i => parameterList.Contains(i.Name)))
//			{
//				Debug.Log("Property: " + p.Name + " : " + p.PropertyType + "; " + p.GetValue(component, null));
//			}
		}

		return JsonUtility.ToJson(modeParams, false);
	}

	private void RestoreMode(string serializedMode)
	{
		// Parse JSON back to objects
		var modeParams = JsonUtility.FromJson<ModeParameters>(serializedMode);
		// Build list that contains Components (only necessary information)
		var trackableComponentsList = modeParams.Parameters.Select(i => Type.GetType(i.ComponentName));
		// Build list of gameObject's components that present in prev list
		var trackableComponents = gameObject.GetComponents(typeof(Component)).Where(c => trackableComponentsList.Contains(c.GetType()));
		// Go through particular gameObject's components only
		foreach (var component in trackableComponents)
		{
			// Get type of component
			Type type = component.GetType();
			// Go through each field in this component
			foreach (var f in type.GetFields().Where(i => i.IsPublic))
			{
				// Find if there is a record for this field in ModeParameters
				var prm = modeParams.Parameters.FirstOrDefault(i => Type.GetType(i.ComponentName) == type && i.ParamName == f.Name);
				// If record was found
				if (prm != default(Parameter))
				{
					// If this is simple type then restore value
					if (prm.ParamType != "CachedUnityObject")
					{
						object obj;

						if (f.FieldType.IsEnum)
						{
							obj = Enum.Parse(f.FieldType, prm.Value);
						}
						else
						{
							obj = Convert.ChangeType(prm.Value, f.FieldType);
						}
						f.SetValue(component, obj);
					}
					// Othervise take object from cache
					else
					{
						var cachedObj = this.cachedUnityObjects.FirstOrDefault(i => i.Key == prm.Value && i.ModeGUID == modeParams.GUID);

						if (cachedObj != default(CachedParameter))
						{
							f.SetValue(component, cachedObj.Value);
						}
					}
				}
			}
		}
	}

	[ContextMenu ("Serialize mode A")]
	void SerializeModeA()
	{
		this.serializedModeA = SerializeCurrentMode("Mode A", this.modeAGUID);
	}

	[ContextMenu ("Serialize mode B")]
	void SerializeModeB()
	{
		this.serializedModeB = SerializeCurrentMode("Mode B", this.modeBGUID);
	}

	[ContextMenu ("Enable mode A")]
	void EnableModeA()
	{
		RestoreMode(this.serializedModeA);
	}

	[ContextMenu ("Enable mode B")]
	void EnableModeB()
	{
		RestoreMode(this.serializedModeB);
	}
}


