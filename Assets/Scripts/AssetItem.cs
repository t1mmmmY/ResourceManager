using UnityEngine;
using System.Collections.Generic;

//[System.Serializable]
public class AssetItem
{
	public string path = "";
	public string tempPath = "";
	public string name = "";
	public bool enabled = true;
	public bool opened = true;
	public bool isFolder = false;
	public List<AssetItem> childAssetItems = new List<AssetItem>();
	
	/// <summary>
	/// Add childs array.
	/// </summary>
	/// <returns><c>true</c>, if child was added, <c>false</c> otherwise.</returns>
	/// <param name="child">Child.</param>
	public bool AddChild(AssetItem[] child)
	{
		childAssetItems = new List<AssetItem>();
		for (int i = 0; i < child.Length; i++)
		{
			AssetItem item = new AssetItem();
			item.name = child[i].name;
			item.path = child[i].path;
			item.enabled = child[i].enabled;
			item.opened = child[i].opened;
			item.isFolder = child[i].isFolder;
			if (child[i].childAssetItems != null)
			{
				item.AddChild(child[i].childAssetItems.ToArray());
			}
			childAssetItems.Add(item);
		}
		
		return childAssetItems.Count > 0;
	}
	
	/// <summary>
	/// Print item with all childs
	/// </summary>
	public void Print()
	{
		ToString();
		
		if (childAssetItems != null)
		{
			foreach (AssetItem item in childAssetItems)
			{
				item.Print();
			}
		}
	}
	
	/// <summary>
	/// Change item and all childs status
	/// </summary>
	/// <param name="status">If set to <c>true</c> status.</param>
	public void RecursivelyChangeStatus(bool status)
	{
		enabled = status;
		
		if (childAssetItems != null)
		{
			foreach (AssetItem item in childAssetItems)
			{
				item.RecursivelyChangeStatus(status);
			}
		}
	}
	
	/// <summary>
	/// Get all child items without folders
	/// </summary>
	/// <returns>The child items.</returns>
	public List<AssetItem> GetChildItems()
	{
		List<AssetItem> items = new List<AssetItem>();
		FillChild(ref items);
		return items;
	}
	
	
	/// <summary>
	/// AssetItem to string.
	/// </summary>
	public override string ToString()
	{
		return string.Format("Name {0}; Path {1}; IsFolder {2}", name, path, isFolder);
	}

	public JSONObject Serialize()
	{
//		JSONObject jsonObject = JSONTemplates.TOJSON(this);
		JSONObject parentObject = new JSONObject(JSONObject.Type.OBJECT);

		JSONObject jsonObject = new JSONObject(JSONObject.Type.OBJECT);
		jsonObject.AddField("name", name);
		jsonObject.AddField("path", path);
		jsonObject.AddField("tempPath", tempPath);
		jsonObject.AddField("enabled", enabled);
		jsonObject.AddField("opened", opened);
		jsonObject.AddField("isFolder", isFolder);

		if (childAssetItems != null)
		{
			for (int i = 0; i < childAssetItems.Count; i++)
			{
				jsonObject.AddField("childAssetItems_" + i.ToString(), childAssetItems[i].Serialize());
			}
		}

		parentObject.AddField("AssetItem", jsonObject);

		return parentObject;
//		Debug.Log(jsonObject.Print(true));
	}

	public void Deserialize(JSONObject jsonObject)
	{
		AccessData(jsonObject, "");
//		return null;
	}

	void AccessData(JSONObject obj, string key)
	{
		switch(obj.type)
		{
		case JSONObject.Type.OBJECT:
			for(int i = 0; i < obj.list.Count; i++)
			{
				string currentKey = (string)obj.keys[i];
				JSONObject j = (JSONObject)obj.list[i];
//				Debug.Log(currentKey);
				if (currentKey.Contains("childAssetItems"))
				{
					if (childAssetItems == null)
					{
						childAssetItems = new List<AssetItem>();
					}
					AssetItem childItem = new AssetItem();
					childItem.Deserialize(j);
					childAssetItems.Add(childItem);
				}
				else
				{
					AccessData(j, currentKey);
				}
			}
			break;
		case JSONObject.Type.ARRAY:
//			Debug.Log("ARRAY");
			foreach(JSONObject j in obj.list)
			{
				AccessData(j, "");
			}
			break;
		case JSONObject.Type.STRING:
			AssignParameter(key, obj);
//			Debug.Log(obj.str);
			break;
		case JSONObject.Type.NUMBER:
			AssignParameter(key, obj);
//			Debug.Log(obj.n);
			break;
		case JSONObject.Type.BOOL:
			AssignParameter(key, obj);
//			Debug.Log(obj.b);
			break;
		case JSONObject.Type.NULL:
			Debug.Log("NULL");
			break;
			
		}
	}

	bool AssignParameter(string key, JSONObject value)
	{
		switch (key)
		{
		case "path":
			path = value.str;
			return true;
//			break;
		case "tempPath":
			tempPath = value.str;
			return true;
//			break;
		case "name":
			name = value.str;
			return true;
//			break;
		case "enabled":
			enabled = value.b;
			return true;
//			break;
		case "opened":
			opened = value.b;
			return true;
//			break;
		case "isFolder":
			isFolder = value.b;
			return true;
//			break;
		default:
			return false;
//			break;
		}
	}
	
	
	/// <summary>
	/// Fill ref array with childs
	/// </summary>
	/// <param name="items">Items.</param>
	private void FillChild(ref List<AssetItem> items)
	{
		if (childAssetItems != null)
		{
			foreach (AssetItem item in childAssetItems)
			{
				if (item.isFolder)
				{
					item.FillChild(ref items);
				}
				else
				{
					items.Add(item);
				}
			}
		}	
	}
	
	
}


