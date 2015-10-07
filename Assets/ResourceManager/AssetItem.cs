using UnityEngine;
using System.Collections.Generic;

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
	public List<AssetItem> GetChildItems(bool includeFolder = false)
	{
		List<AssetItem> items = new List<AssetItem>();
		FillChild(ref items, includeFolder);
		return items;
	}

	/// <summary>
	/// AssetItem to string.
	/// </summary>
	public override string ToString()
	{
		return string.Format("Name {0}; Path {1}; IsFolder {2}", name, path, isFolder);
	}

	/// <summary>
	/// Fill ref array with childs
	/// </summary>
	/// <param name="items">Items.</param>
	private void FillChild(ref List<AssetItem> items, bool includeFolder = false)
	{
		if (childAssetItems != null)
		{
			foreach (AssetItem item in childAssetItems)
			{
				if (item.isFolder)
				{
					item.FillChild(ref items);
					if (includeFolder)
					{
						items.Add(item);
					}
				}

				items.Add(item);
			}
		}	
	}
}


