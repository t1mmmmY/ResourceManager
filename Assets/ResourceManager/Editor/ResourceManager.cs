using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;

public class ResourceManager : EditorWindow
{
	//Modify these parameters as you want
	static string saveDataPath = "SaveData.txt";
//	static string tempFolderPath = "Assets/_Resources/";
//	static string tempFolderAssetDatabese = "_Resources";
	
	static string tempFolderPath = "../_Resources/";
//	static string tempFolderAssetDatabese = "_Resources";

	static List<AssetItem> assets;
	static List<AssetItem> savedAssets;

	static Rect toggleRect;
	static bool cleared = false;
	static bool changed = false;

	static bool init = false;

	static Vector2 scrollPosition = Vector2.zero;
	static string buildPath;

	[MenuItem("Resource Manager/Edit...")]
	static void Init() 
	{
		ResourceManager window = (ResourceManager)EditorWindow.GetWindow (typeof (ResourceManager));
		window.Show();
	}



	static void LoadAssets()
	{
		string[] rootPaths = AssetDatabase.FindAssets("Resources");
		assets = new List<AssetItem>();

		for (int i = 0; i < rootPaths.Length; i++)
		{
			rootPaths[i] = AssetDatabase.GUIDToAssetPath(rootPaths[i]);//.Replace("Assets/", "");
			AssetItem rootItem = new AssetItem();
			rootItem.path = rootPaths[i];
			rootItem.name = rootItem.path.Substring(rootItem.path.LastIndexOf('/') + 1);
			rootItem.isFolder = true;
			rootItem.AddChild(GetSubAssets(rootItem.path));
			assets.Add(rootItem);
		}

		if (File.Exists(GetAbsolutePath(saveDataPath)))
		{
			LoadData();
		}

		init = true;
	}

	static AssetItem[] GetSubAssets(string path)
	{
		string[] allSubAssets = AssetDatabase.FindAssets("", new string[] { path } );
		for (int i = 0; i < allSubAssets.Length; i++)
		{
			allSubAssets[i] = AssetDatabase.GUIDToAssetPath(allSubAssets[i]);
		}

		List<AssetItem> subAssets = new List<AssetItem>();

		//Find folders
		foreach (string asset in allSubAssets)
		{
			int nextSlashIndex = -1;
			if (asset.Length > path.Length + 1)
			{
				nextSlashIndex = asset.IndexOf('/', path.Length + 1);
			}
//			try
//			{
//				nextSlashIndex = asset.IndexOf('/', path.Length + 1);
//			}
//			catch (System.Exception ex)
//			{
//				int k = 0;
//			}
			AssetItem item = new AssetItem();

			if (nextSlashIndex != -1)
			{
				//Folder
				item.path = asset.Substring(0, nextSlashIndex);
				item.name = item.path.Substring(item.path.LastIndexOf('/') + 1);
				item.isFolder = true;

				if (!ContainsAssetWithTheSamePath(item.path, subAssets.ToArray()))
				{
					subAssets.Add(item);
				}
			}

		}

		//Find items
		foreach (string asset in allSubAssets)
		{
			int nextSlashIndex = -1;
			if (asset.Length > path.Length + 1)
			{
				nextSlashIndex = asset.IndexOf('/', path.Length + 1);
			}
			
			AssetItem item = new AssetItem();

			if (nextSlashIndex == -1)
			{
				//Some object
				item.path = asset;
				item.name = item.path.Substring(item.path.LastIndexOf('/') + 1);
				item.isFolder = false;

				if (!ContainsAssetWithTheSamePath(item.path, subAssets.ToArray()))
				{
					subAssets.Add(item);
				}
			}
			

		}

		for (int i = 0; i < subAssets.Count; i++)
		{
			if (subAssets[i].isFolder)
			{
				subAssets[i].AddChild(GetSubAssets(subAssets[i].path));
			}
		}

		return subAssets.ToArray();
	}

	static bool ContainsAssetWithTheSamePath(string path, AssetItem[] items)
	{
		foreach (AssetItem item in items)
		{
			if (item.path == path)
			{
				return true;
			}
		}
		return false;
	}

	static AssetItem GetItemWithTheSamePath(string path, AssetItem[] items)
	{
		foreach (AssetItem item in items)
		{
			if (item.path == path)
			{
				return item;
			}
		}
		return null;
	}

	void OnGUI() 
	{
		//If initialize already
		if (assets != null)
		{
			changed = false;
			toggleRect = new Rect(3, 3, 15, 15);


			int countItems = GetAllItems(assets, true).Length;
			scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), 
			                                     scrollPosition, 
			                                     new Rect(0, 0, position.width - 20, countItems * 20));
			{
				//Draw root resources folders
				foreach (AssetItem item in assets)
				{
					float oldX = toggleRect.x;
					DrawAssets(item, false);
					toggleRect.x = oldX;
					toggleRect.y += 18;
				}
			}
			GUI.EndScrollView();

			if (changed)
			{
				SaveData();
			}
		}


		//FOR TESTING
//		if (!cleared)
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 120, 80, 30), "Clear"))
			{
				HideUnusedAssets();
			}
		}
//		else
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 80, 80, 30), "Restore"))
			{
				RestoreUnusedAssets();
			}
		}


		if (!cleared)
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 40, 80, 30), "Refresh"))
			{
				LoadAssets();
			}
		}

	}

	void DrawAssets(AssetItem item, bool child)
	{
		DrawItem(item, child);
		if (item.childAssetItems != null)
		{
			if (item.opened)
			{
				foreach (AssetItem childItem in item.childAssetItems)
				{
					toggleRect.y += 18;
					float oldX = toggleRect.x;
					DrawAssets(childItem, true);
					toggleRect.x = oldX;
				}
			}
		}
	}

	void DrawItem(AssetItem item, bool child)
	{
		if (child)
		{
			toggleRect.x += 20;
		}
		Rect foldoutRect = toggleRect;
		foldoutRect.x += toggleRect.width + 3;
		foldoutRect.width = position.width - (foldoutRect.x + 3); 

		if (!cleared)
		{
			bool oldEnabledStatus = item.enabled;
			item.enabled = EditorGUI.Toggle(toggleRect, item.enabled);
			if (item.enabled != oldEnabledStatus)
			{
				item.RecursivelyChangeStatus(item.enabled);
				changed = true;
			}
		}
		else
		{
			EditorGUI.Toggle(toggleRect, item.enabled);
		}

		if (item.isFolder)
		{
			item.opened = EditorGUI.Foldout(foldoutRect, item.opened, item.name);
		}
		else
		{
			Rect imageRect = toggleRect;
			imageRect.x += 20;
			foldoutRect.x += 20;
			Texture tex = AssetDatabase.GetCachedIcon(item.path);
			if (tex != null)
			{
				EditorGUI.DrawPreviewTexture(imageRect, tex);
			}
			EditorGUI.LabelField(foldoutRect, item.name);
		}
	}

	static string GetAbsolutePath(string localPath)
	{
		string absolutePath = Application.dataPath + "/" + localPath;
		Debug.Log(absolutePath);
		return absolutePath;
	}

	static void HideUnusedAssets()
	{
		AssetItem[] allAssets = GetAllItems(assets);
		List<AssetItem> unusedAssets = new List<AssetItem>();
//		AssetDatabase.CreateFolder("Assets", tempFolderPath);

		Directory.CreateDirectory(GetAbsolutePath(tempFolderPath));
//		AssetDatabase.CreateFolder("Assets", tempFolderAssetDatabese);
		//tempFolderPath
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled)
			{
				unusedAssets.Add(item);

				string newPath = string.Format("{0}{1}", GetAbsolutePath(tempFolderPath), item.name);
				File.Move(item.path, newPath);
//				string result = AssetDatabase.MoveAsset(item.path, newPath);
//				if (result != "")
//				{
//					Debug.LogWarning(result);
//				}
				item.tempPath = newPath;
			}
		}

		AssetDatabase.Refresh();
		SaveData();

		cleared = true;
	}
	
	static void RestoreUnusedAssets()
	{
		LoadData();
		AssetItem[] allAssets = GetAllItems(assets);
		
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled)
			{
				File.Move(item.tempPath, GetAbsolutePath(item.path.Replace("Assets/", "")));
//				AssetDatabase.MoveAsset(item.tempPath, item.path);
				item.tempPath = "";
			}
		}

//		File.Delete(Application.dataPath + tempFolderPath);
//		AssetDatabase.DeleteAsset(tempFolderPath.Remove(tempFolderPath.Length - 1));
		AssetDatabase.Refresh();
		SaveData();
		
		cleared = false;

	}

	static AssetItem[] GetAllItems(List<AssetItem> baseAssets, bool includeFolder = false)
	{
		List<AssetItem> childItems = new List<AssetItem>();
		foreach (AssetItem item in baseAssets)
		{
			childItems.AddRange(item.GetChildItems(includeFolder));
		}

		return childItems.ToArray();
	}


	void OnInspectorUpdate() 
	{
		if (!init)
		{
			LoadAssets();
		}

		Repaint();
	}

	static void SaveData()
	{
		StringBuilder outputString = new StringBuilder();
		for (int i = 0; i < assets.Count; i++)
		{
			outputString.Append(assets[i].Serialize().Print(true));

			if (i < assets.Count - 1)
			{
				outputString.Append(',');
				outputString.AppendLine();
			}
		}

		File.WriteAllText(GetAbsolutePath(saveDataPath), outputString.ToString());
	}

	static void LoadData()
	{
		savedAssets = new List<AssetItem>();
		string jsonText = File.ReadAllText(GetAbsolutePath(saveDataPath));
		JSONObject jsonObject = new JSONObject(jsonText);
		AccessData(jsonObject);

		AssetItem[] allSavedItems;
		AssetItem[] allItems;
		allSavedItems = GetAllItems(savedAssets, true);
		allItems = GetAllItems(assets, true);
		//Get all saved items

		foreach (AssetItem item in allSavedItems)
		{
			if (ContainsAssetWithTheSamePath(item.path, allItems))
			{
				//Item exist
//				Debug.Log("Item exist " + item.path);
				GetItemWithTheSamePath(item.path, allItems).enabled = GetItemWithTheSamePath(item.path, allSavedItems).enabled;
			}
			else
			{
				//Item not exist
			}
		}

		SaveData();

	}

	static void AccessData(JSONObject obj)
	{
		switch(obj.type)
		{
		case JSONObject.Type.OBJECT:
			for(int i = 0; i < obj.list.Count; i++)
			{
				JSONObject j = (JSONObject)obj.list[i];
				AssetItem item = new AssetItem();
				item.Deserialize(j);
				savedAssets.Add(item);
			}
			break;
		default: 
			Debug.LogWarning("It is not an object!");
			break;
			
		}
	}



	[MenuItem("Resource Manager/Build")]
	public static void BuildGame ()
	{
		EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
		List<string> scenesPath = new List<string>();
		foreach(EditorBuildSettingsScene scene in scenes)
		{
			if (scene.enabled)
			{
				scenesPath.Add(scene.path);
			}
		}

		if (PlayerPrefs.HasKey("BUILD_PATH"))
		{
			buildPath = PlayerPrefs.GetString("BUILD_PATH");
		}
		// Get filename.
		buildPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", buildPath, "", "");

		if (buildPath != string.Empty)
		{
			Hide();

			PlayerPrefs.SetString("BUILD_PATH", buildPath);
			// Build player.
			BuildPipeline.BuildPlayer(scenesPath.ToArray(), buildPath, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);

			Restore();
		}
	}

	static void Hide()
	{
		LoadAssets();
		HideUnusedAssets();
	}
	
	static void Restore()
	{
		RestoreUnusedAssets();
	}


	void OnDestroy()
	{
		if (cleared)
		{
			RestoreUnusedAssets();
		}
	}

}