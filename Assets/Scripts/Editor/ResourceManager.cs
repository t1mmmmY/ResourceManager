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
	static string saveDataPath = "/SaveData.txt";
//	string saveDataName = "SaveData";
	static string tempFolderPath = "Assets/_Resources/";

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

//	[MenuItem("Window/Resource Manager/Hide Unused Assets")]
	static void Hide()
	{
		LoadAssets();
		HideUnusedAssets();
	}

//	[MenuItem("Window/Resource Manager/Restore Unused Assets")]
	static void Restore()
	{
//		LoadAssets();
		RestoreUnusedAssets();
	}

	static void LoadAssets()
	{
		string[] rootPaths = AssetDatabase.FindAssets("Resources");
		assets = new List<AssetItem>();

		for (int i = 0; i < rootPaths.Length; i++)
		{
			rootPaths[i] = AssetDatabase.GUIDToAssetPath(rootPaths[i]);
			AssetItem rootItem = new AssetItem();
			rootItem.path = rootPaths[i];
			rootItem.name = rootItem.path.Substring(rootItem.path.LastIndexOf('/') + 1);
			rootItem.isFolder = true;
			rootItem.AddChild(GetSubAssets(rootItem.path));
			assets.Add(rootItem);
		}

		if (File.Exists(Application.dataPath + saveDataPath))
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
			int nextSlashIndex = asset.IndexOf('/', path.Length + 1);
			
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
			int nextSlashIndex = asset.IndexOf('/', path.Length + 1);
			
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
//				Debug.Log("SAVE DATA on change");
				SaveData();
			}
		}


		if (!cleared)
		{
//			if (GUI.Button(new Rect(position.width - 90, position.height - 130, 80, 30), "Load Data"))
//			{
//				LoadData();
//			}

			if (GUI.Button(new Rect(position.width - 90, position.height - 40, 80, 30), "Refresh"))
			{
				LoadAssets();
			}
		}

//		if (!cleared)
//		{
//			if (GUI.Button(new Rect(position.width - 90, position.height - 80, 80, 30), "Clear"))
//			{
//				HideUnusedAssets();
//			}
//		}
//		else
//		{
//			if (GUI.Button(new Rect(position.width - 90, position.height - 80, 80, 30), "Restore"))
//			{
//				RestoreUnusedAssets();
//			}
//		}

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

	static void HideUnusedAssets()
	{
		AssetItem[] allAssets = GetAllItems(assets);
		List<AssetItem> unusedAssets = new List<AssetItem>();
		AssetDatabase.CreateFolder("Assets", "_Resources");
		
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled)
			{
				unusedAssets.Add(item);
//				Debug.Log(item.ToString());

				string newPath = string.Format("{0}{1}", tempFolderPath, item.name);
				string result = AssetDatabase.MoveAsset(item.path, newPath);
				if (result != "")
				{
					Debug.LogWarning(result);
				}
				item.tempPath = newPath;
			}
		}

		AssetDatabase.Refresh();
//		Debug.Log("SAVE DATA on hide assets");
		SaveData();

		cleared = true;
	}
	
	static void RestoreUnusedAssets()
	{
		LoadData();
		AssetItem[] allAssets = GetAllItems(assets);
//		List<AssetItem> unusedAssets = new List<AssetItem>();
		
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled)
			{
//				unusedAssets.Add(item);
				//				Debug.Log(item.ToString());
				
				AssetDatabase.MoveAsset(item.tempPath, item.path);
				item.tempPath = "";
			}
		}
		
		AssetDatabase.DeleteAsset(tempFolderPath.Remove(tempFolderPath.Length - 1));
		AssetDatabase.Refresh();
//		Debug.Log("SAVE DATA on restore");
		SaveData();
		
		cleared = false;

//		AssetItem[] allAssets = GetAllItems(assets);
//		List<AssetItem> unusedAssets = new List<AssetItem>();
//		
//		foreach (AssetItem item in allAssets)
//		{
//			if (!item.enabled)
//			{
//				unusedAssets.Add(item);
////				Debug.Log(item.ToString());
//
//				AssetDatabase.MoveAsset(item.tempPath, item.path);
//				item.tempPath = "";
//			}
//		}
//
//		AssetDatabase.DeleteAsset(tempFolderPath.Remove(tempFolderPath.Length - 1));
//		AssetDatabase.Refresh();
//
//		cleared = false;
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

//		Debug.Log(Application.dataPath + saveDataPath);
		File.WriteAllText(Application.dataPath + saveDataPath, outputString.ToString());
	}

	static void LoadData()
	{
		savedAssets = new List<AssetItem>();
		string jsonText = File.ReadAllText(Application.dataPath + saveDataPath);
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


//				AssetItem savedItem = GetItemWithTheSamePath(item.path, allSavedItems);
//				if (savedItem.tempPath != "")
//				{
//					assets.Add(savedItem);
//				}
			}
		}

//		Debug.Log("SAVE DATA on load data");
		SaveData();
		
//		for(int i = 0; i < jsonObject.list.Count; i++)
//		{
//			string key = (string)jsonObject.keys[i];
//			JSONObject j = (JSONObject)jsonObject.list[i];
//			Debug.Log(key);
//
//		}
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
//				string key = (string)obj.keys[i];
//				JSONObject j = (JSONObject)obj.list[i];
//				Debug.Log(obj.list[i].Print(true));
//				AccessData(j);
			}
			break;
		default: 
			Debug.LogWarning("It is not an object!");
			break;
//		case JSONObject.Type.ARRAY:
//			Debug.Log("ARRAY");
//			foreach(JSONObject j in obj.list)
//			{
//				AccessData(j);
//			}
//			break;
//		case JSONObject.Type.STRING:
//			Debug.Log(obj.str);
//			break;
//		case JSONObject.Type.NUMBER:
//			Debug.Log(obj.n);
//			break;
//		case JSONObject.Type.BOOL:
//			Debug.Log(obj.b);
//			break;
//		case JSONObject.Type.NULL:
//			Debug.Log("NULL");
//			break;
			
		}
	}

//	[PostProcessBuildAttribute()]
//	static void RestoreAssetsAfterBuild()
//	{
//		//This one will execute after build process
//		init = false;
//	}


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

		Hide ();

		if (buildPath != string.Empty)
		{
			PlayerPrefs.SetString("BUILD_PATH", buildPath);
			// Build player.
			BuildPipeline.BuildPlayer(scenesPath.ToArray(), buildPath, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);
		}

		Restore();
	}


	void OnDestroy()
	{
		if (cleared)
		{
			RestoreUnusedAssets();
		}
	}

	void Update()
	{
		if (BuildPipeline.isBuildingPlayer)
		{
			Debug.Log("BUILDING");
			HideUnusedAssets();
		}
	}
}