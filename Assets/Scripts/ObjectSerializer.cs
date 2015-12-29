using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.ComponentModel;

[Serializable]
public class TargetParameters
{
	public UnityEngine.Component Component;
	public ComponentProperties property;

	public void SetPropertiesList(string[] propertyNames)
	{
		property.SetProperties(propertyNames);
	}
}

[Serializable]
public class Parameter
{
	public string ComponentName;
	public string ParamName;
	public string ParamType;
	public string Value;

	public override string ToString ()
	{
		return string.Format ("<{0}>.{1} ({2}) = '{3}'", ComponentName, ParamName, ParamType, Value);
	}
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

	void Update()
	{
		if (!Application.isPlaying)
		{
			foreach (TargetParameters parameter in trackableParameters)
			{
				parameter.SetPropertiesList(InspectComponent(parameter.Component));
			}
		}
	}
		
	[ContextMenu ("Inspect Object")]
	void InspectObject()
	{
		foreach (var component in gameObject.GetComponents(typeof(UnityEngine.Component)))
		{
			InspectComponent(component, true);
		}
	}

	string[] InspectComponent(UnityEngine.Component component, bool log = false)
	{
		Type type = component.GetType();
		if (log)
		{
			Debug.Log(">> " + type);
		}

		List<string> propertyNames = new List<string>();

		foreach (var f in type.GetFields().Where(f => f.IsPublic))
		{
			bool fieldHasConverter = TypeDescriptor.GetConverter(f.FieldType).CanConvertFrom(typeof(String));
			bool fieldIsUnityObjectSubclass = f.FieldType.IsSubclassOf(typeof(UnityEngine.Object));
			bool fieldTypeIsWrappedProperly = f.FieldType == typeof(Vector3) || f.FieldType == typeof(Quaternion);

			if (fieldHasConverter || fieldIsUnityObjectSubclass || fieldTypeIsWrappedProperly)
			{
				propertyNames.Add(f.Name);

				if (log)
				{
					Debug.Log("Field: " + f.Name + " : " + f.FieldType + "; " + f.GetValue(component));
				}
			}
		}

		var ignoredProperties = new List<String>();
		ignoredProperties.Add("rigidbody");
		ignoredProperties.Add("rigidbody2D");
		ignoredProperties.Add("camera");
		ignoredProperties.Add("light");
		ignoredProperties.Add("animation");
		ignoredProperties.Add("constantForce");
		ignoredProperties.Add("renderer");
		ignoredProperties.Add("audio");
		ignoredProperties.Add("guiText");
		ignoredProperties.Add("networkView");
		ignoredProperties.Add("guiElement");
		ignoredProperties.Add("guiTexture");
		ignoredProperties.Add("collider");
		ignoredProperties.Add("collider2D");
		ignoredProperties.Add("hingeJoint");
		ignoredProperties.Add("particleEmitter");
		ignoredProperties.Add("particleSystem");

		// Ignore this for now
		ignoredProperties.Add("mesh");
		ignoredProperties.Add("material");
		ignoredProperties.Add("materials");
		ignoredProperties.Add("transform");
		ignoredProperties.Add("gameObject");

		foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(i => !ignoredProperties.Contains(i.Name)))
		{
			bool propHasConverter = TypeDescriptor.GetConverter(p.PropertyType).CanConvertFrom(typeof(String));
			bool propIsUnityObjectSubclass = p.PropertyType.IsSubclassOf(typeof(UnityEngine.Object));
			bool propTypeIsWrappedProperly = p.PropertyType == typeof(Vector3) || p.PropertyType == typeof(Quaternion);

			if (propHasConverter || propIsUnityObjectSubclass || propTypeIsWrappedProperly)
			{
				propertyNames.Add(p.Name);

				if (log)
				{
					Debug.Log("Property: '" + p.Name + "' [" + p.PropertyType + "] Value: " + p.GetValue(component, null));
				}
			}
		}

		return propertyNames.ToArray();
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
		var trackComponents = gameObject.GetComponents(typeof(UnityEngine.Component)).Where(c => trackComponentsList.Contains(c));
		// Go through particular gameObject's components only
		foreach (var component in trackComponents)
		{
			// Get type of component
			Type type = component.GetType();
			// Build list of required parameter names. To avoid reading all the properties
			var parameterList = this.trackableParameters.Where(i => i.Component == component).Select(i => i.property.ParamName);
			// Read only required parameters of gameObject's component
			foreach (var f in type.GetFields().Where(i => i.IsPublic && parameterList.Contains(i.Name)))
			{
				bool fieldHasConverter = TypeDescriptor.GetConverter(f.FieldType).CanConvertFrom(typeof(String));
				bool fieldIsUnityObjectSubclass = f.FieldType.IsSubclassOf(typeof(UnityEngine.Object));
				bool fieldTypeIsWrappedProperly = f.FieldType == typeof(Vector3) || f.FieldType == typeof(Quaternion);

				// Simple types derived from struct and string serialize as is
				if (fieldHasConverter || fieldTypeIsWrappedProperly)
				{
					string paramValue = string.Empty;
					if (f.FieldType == typeof(Vector3)) 
					{
						paramValue = UnityTypesConverter.Vector3ToString(((Vector3)f.GetValue(component)));
					}
					else if (f.FieldType == typeof(Quaternion))
					{
						paramValue = UnityTypesConverter.QuaternionToString(((Quaternion)f.GetValue(component)));
					}
					else
					{
						paramValue = TypeDescriptor.GetConverter(f.FieldType).ConvertToString(f.GetValue(component));
					}

					Parameter param = new Parameter();
					param.ComponentName = type.ToString();
					param.ParamName = f.Name;
					param.ParamType = f.FieldType.ToString();
					param.Value = paramValue;
					modeParams.Parameters.Add(param);
				}
				// UnityEngine objects save in cachedUnityObjects list which is serialized by Unity in UnityEditor
				if (fieldIsUnityObjectSubclass)
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
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			foreach (var p in type.GetProperties(flags).Where(i => parameterList.Contains(i.Name)))
			{
				bool propHasConverter = TypeDescriptor.GetConverter(p.PropertyType).CanConvertFrom(typeof(String));
				bool propIsUnityObjectSubclass = p.PropertyType.IsSubclassOf(typeof(UnityEngine.Object));
				bool propTypeIsWrappedProperly = p.PropertyType == typeof(Vector3) || p.PropertyType == typeof(Quaternion);

				// Simple types derived from struct and string serialize as is
				if (propHasConverter || propTypeIsWrappedProperly)
				{
					string paramValue = string.Empty;
					if (p.PropertyType == typeof(Vector3)) 
					{
						paramValue = UnityTypesConverter.Vector3ToString(((Vector3)p.GetValue(component, null)));
					}
					else if (p.PropertyType == typeof(Quaternion))
					{
						paramValue = UnityTypesConverter.QuaternionToString(((Quaternion)p.GetValue(component, null)));
					}
					else
					{
						paramValue = TypeDescriptor.GetConverter(p.PropertyType).ConvertToString(p.GetValue(component, null));
					}

					Parameter param = new Parameter();
					param.ComponentName = type.ToString();
					param.ParamName = p.Name;
					param.ParamType = p.PropertyType.ToString();
					param.Value = paramValue;

					modeParams.Parameters.Add(param);
				}
				// UnityEngine objects save in cachedUnityObjects list which is serialized by Unity in UnityEditor
				if (propIsUnityObjectSubclass)
				{
					Parameter param = new Parameter();
					param.ComponentName = type.ToString();
					param.ParamName = p.Name;
					param.ParamType = "CachedUnityObject";
					param.Value = type + "." + p.Name;

					// Remove old parameter for this mode
					this.cachedUnityObjects.RemoveAll(i => i.Key == param.Value && i.ModeGUID == modeGUID);
					// Add new parameter for this mode
					this.cachedUnityObjects.Add(new CachedParameter(param.Value, 
						modeGUID,
						(UnityEngine.Object)p.GetValue(component, null)));
					modeParams.Parameters.Add(param);
				}			
			}
		}

		return JsonUtility.ToJson(modeParams, false);
	}

	private void RestoreMode(string serializedMode)
	{
		// Parse JSON back to objects
		var modeParams = JsonUtility.FromJson<ModeParameters>(serializedMode);
		// Build list that contains Components (only necessary information)
		var trackableComponentsList = modeParams.Parameters.Select(i => i.ComponentName);
		// Build list of gameObject's components that present in prev list
		var trackableComponents = gameObject.GetComponents(typeof(UnityEngine.Component)).Where(c => trackableComponentsList.Contains(c.GetType().ToString()));
		// Go through particular gameObject's components only
		foreach (var component in trackableComponents)
		{
			// Get type of component
			Type type = component.GetType();

			// Go through each field in this component
			foreach (var f in type.GetFields().Where(i => i.IsPublic))
			{
				// Find if there is a record for this field in ModeParameters
				var prm = modeParams.Parameters.FirstOrDefault(i => i.ComponentName == type.ToString() && i.ParamName == f.Name);
				// If record was found
				if (prm != default(Parameter))
				{
					// If this is simple type then restore value
					if (prm.ParamType != "CachedUnityObject")
					{
						object obj = null;
						var converter = TypeDescriptor.GetConverter(f.FieldType);

						if (!converter.CanConvertFrom(typeof(String)))
						{
							if (f.FieldType == typeof(Vector3))
							{
								obj = UnityTypesConverter.Vector3FromString(prm.Value);
							}
							if (f.FieldType == typeof(Quaternion))
							{
								obj = UnityTypesConverter.QuaternionFromString(prm.Value);
							}
						}
						else
						{
							obj = converter.ConvertFromString(prm.Value);
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

			// Go through each property in this component
			BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
			foreach (var p in type.GetProperties(flags))
			{
				// Find if there is a record for this field in ModeParameters
				var prm = modeParams.Parameters.FirstOrDefault(i => i.ComponentName == type.ToString() && i.ParamName == p.Name);
				// If record was found
				if (prm != default(Parameter))
				{
					// If this is simple type then restore value
					if (prm.ParamType != "CachedUnityObject")
					{
						object obj = null;
						var converter = TypeDescriptor.GetConverter(p.PropertyType);

						if (!converter.CanConvertFrom(typeof(String)))
						{
							if (p.PropertyType == typeof(Vector3))
							{
								obj = UnityTypesConverter.Vector3FromString(prm.Value);
							}
							if (p.PropertyType == typeof(Quaternion))
							{
								obj = UnityTypesConverter.QuaternionFromString(prm.Value);
							}
						}
						else
						{
							obj = converter.ConvertFromString(prm.Value);
						}
							
						p.SetValue(component, obj, null);
					}
					// Othervise take object from cache
					else
					{
						var cachedObj = this.cachedUnityObjects.FirstOrDefault(i => i.Key == prm.Value && i.ModeGUID == modeParams.GUID);

						if (cachedObj != default(CachedParameter))
						{
							p.SetValue(component, cachedObj.Value, null);
						}
					}
				}
			}
		}
	}

	[ContextMenu ("Serialize mode A")]
	public void SerializeModeA()
	{
		this.serializedModeA = SerializeCurrentMode("Mode A", this.modeAGUID);
	}

	[ContextMenu ("Serialize mode B")]
	public void SerializeModeB()
	{
		this.serializedModeB = SerializeCurrentMode("Mode B", this.modeBGUID);
	}

	[ContextMenu ("Enable mode A")]
	public void EnableModeA()
	{
		RestoreMode(this.serializedModeA);
	}

	[ContextMenu ("Enable mode B")]
	public void EnableModeB()
	{
		RestoreMode(this.serializedModeB);
	}
}


