//#define RESOURCE_MANAGER_TEST

using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class ResourceManager : EditorWindow
{
	//Modify these parameters as you want
	static string _saveDataPath = "SaveData.txt";
	static string _tempFolderName = "TempResources";

	static List<AssetItem> _assets;
	static List<AssetItem> _savedAssets;

	static Rect toggleRect;
	static bool cleared
	{ 
		get 
		{
			return PlayerPrefs.GetInt("IS_CLEARED", 0) == 1;
		}
		set
		{
			PlayerPrefs.SetInt("IS_CLEARED", (value == true) ? 1 : 0);
			PlayerPrefs.Save();
		}
	}

	static bool changed = false;
	static bool init = false;
	static Vector2 scrollPosition = Vector2.zero;

	[MenuItem("Resource Manager/Edit...")]
	static void Init() 
	{
		Refresh();
		ResourceManager window = (ResourceManager)EditorWindow.GetWindow (typeof (ResourceManager));
		window.Show();
	}

	static void Refresh()
	{
		LoadAssets();
	}

	static void LoadAssets()
	{
		// Get all assets that contained in different Resources folders
		var rootPaths = AssetDatabase.FindAssets("Resources").Select(item => AssetDatabase.GUIDToAssetPath(item));
		_assets = new List<AssetItem>();

		foreach(var path in rootPaths)
		{
			var dirInfo = new DirectoryInfo(path);
			// Exclude temporary folder if it contains "Resources" word
			if (!string.Equals(dirInfo.Name, _tempFolderName))
			{
				var rootItem = new AssetItem();

				rootItem.path = path;
				rootItem.name = dirInfo.Name;
				rootItem.isFolder = true;
				rootItem.AddChild(GetSubAssets(rootItem.path));

				_assets.Add(rootItem);
			}
		}

		if (File.Exists(GetAbsolutePath(_saveDataPath)))
		{
			if (!cleared)
			{
				LoadData();
			}
		}

		init = true;
	}

	static AssetItem[] GetSubAssets(string path)
	{
		// Get all assets contains in folder with "path" path
		var allSubAssets = AssetDatabase.FindAssets("", new string[] { path }).Select(item => AssetDatabase.GUIDToAssetPath(item));
		//Select assets that are in current folder (path) only. No subfolders and other folders
		allSubAssets = allSubAssets.Where(item => string.Equals(Path.GetDirectoryName(item), path)).Distinct<string>();
		var subAssets = new List<AssetItem>();

		//Find content
		foreach(var asset in allSubAssets)
		{
			FileAttributes attr = File.GetAttributes(@asset);
			bool isDirectory = (attr & FileAttributes.Directory) == FileAttributes.Directory;

			var item = new AssetItem();
			item.isFolder = isDirectory;

			if (isDirectory)
			{
				var dirInfo = new DirectoryInfo(@asset);
				item.path = asset;
				item.name = dirInfo.Name;
				
				//Add items recursively (folders and content)
				item.AddChild(GetSubAssets(item.path));
			}
			else
			{
				var fileInfo = new FileInfo(@asset);
				item.path = asset;
				item.name = fileInfo.Name;
			}

			subAssets.Add(item);
        }

		return subAssets.ToArray();
	}

	static bool ContainsAssetWithTheSamePath(string path, AssetItem[] items)
	{
		return items.Any(i => i.path == path);
	}

	static AssetItem GetItemWithTheSamePath(string path, AssetItem[] items)
	{
		return items.FirstOrDefault(i => i.path == path);
	}

	void OnGUI() 
	{
		//If initialize already
		if (_assets != null)
		{
			changed = false;
			toggleRect = new Rect(3, 3, 15, 15);


			int countItems = GetAllItems(_assets, true).Length;
			scrollPosition = GUI.BeginScrollView(new Rect(0, 0, position.width, position.height), 
			                                     scrollPosition, 
			                                     new Rect(0, 0, position.width - 20, countItems * 20));
			{
				//Draw root resources folders
				foreach (AssetItem item in _assets)
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

#if RESOURCE_MANAGER_TEST
		//Clear/Restore pair of buttons
		if (!cleared)
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 40, 80, 30), "Clear"))
			{
				HideUnusedAssets();
			}
		}
		else
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 40, 80, 30), "Restore"))
			{
				RestoreUnusedAssets();
			}
		}


		//Refresh/Check dependencies pair of buttons
		if (!cleared)
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 80, 80, 30), "Refresh"))
			{
				Refresh();
			}
		}
#else
		//Refresh/Check dependencies pair of buttons
		if (!cleared)
		{
			if (GUI.Button(new Rect(position.width - 90, position.height - 40, 80, 30), "Refresh"))
			{
				Refresh();
			}
		}
#endif
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

		if (!new Rect(scrollPosition, position.size).Contains(toggleRect.position))
		{
			return;
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
		//IF localPath relative and starts with "Assets/"
		if (localPath.StartsWith("Assets" + Path.DirectorySeparatorChar))
		{
			// Example /Users/{UserName}/{ProjectName}
			string assetParentFolder = Directory.GetParent(Application.dataPath).FullName;
			// Example /Users/{UserName}/{ProjectName}/{localPath}
			return Path.Combine(assetParentFolder, localPath);
		}
		else
		{
			return Path.Combine(Application.dataPath, localPath);
		}
	}

	static string GetRelativePath(string localPath)
	{
		return Path.Combine("Assets", localPath);
	}

	/// <summary>
	/// Hides the unused assets in temporary folder using AssetDatabase class.
	/// </summary>
	static void HideUnusedAssets()
	{
		AssetItem[] allAssets = GetAllItems(_assets);
		List<AssetItem> unusedAssets = new List<AssetItem>();

		//Moving unchecked items to temporary folder
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled && !item.isFolder)
            {
				unusedAssets.Add(item);

				// Example /Users/{UserName}/{ProjectName}
				string assetParentAbsPath = Directory.GetParent(Application.dataPath).FullName;
				// Put Folder out of Unity's Asset folder
				// Example /Users/{UserName}/{ProjectName}/{TempFolderName} 
				string tempFolderAbsPath = Path.Combine(assetParentAbsPath, _tempFolderName);
				// Example /Users/{UserName}/{ProjectName}/{TempFolderName}/{FileName.xxx}
				string itemRandomName = Path.GetRandomFileName();
				string newAbsPath = Path.Combine(tempFolderAbsPath, itemRandomName);

				if (!Directory.Exists(tempFolderAbsPath))
				{
					Directory.CreateDirectory(tempFolderAbsPath);
				}

				try
				{
					string oldItemPath = GetAbsolutePath(item.path);

					// Move file itself
					if (File.Exists(oldItemPath))
					{
						File.Move(oldItemPath, newAbsPath);
					}
					// Move its .meta file
					if (File.Exists(oldItemPath + ".meta"))
					{
						File.Move(oldItemPath + ".meta", newAbsPath + ".meta");
					}

					item.tempPath = newAbsPath;
				}
				catch (System.UnauthorizedAccessException ex)
				{
					Debug.LogError(ex.Message);
				}
				catch (System.IO.IOException ex)
				{
					Debug.LogError(ex.Message);
				}
			}
		}

		AssetDatabase.Refresh();
		SaveData();

		cleared = true;
	}


	static void RestoreUnusedAssets()
	{
		LoadData();
		AssetItem[] allAssets = GetAllItems(_savedAssets);
		
		foreach (AssetItem item in allAssets)
		{
			if (!item.enabled && !item.isFolder)
            {
				try
				{
					// Move file's .meta file
					if (File.Exists(item.tempPath + ".meta"))
					{
						File.Move(item.tempPath + ".meta", item.path + ".meta");
                    }
					// Move file itself
					if (File.Exists(item.tempPath))
					{
						File.Move(item.tempPath, item.path);
					}
				}
				catch (System.UnauthorizedAccessException ex)
				{
					Debug.LogError(ex.Message);
				}
				catch (System.IO.IOException ex)
				{
					Debug.LogError(ex.Message);
				}

				item.tempPath = "";
			}
		}

		AssetDatabase.Refresh(ImportAssetOptions.ForceUncompressedImport);

		cleared = false;
		LoadAssets();
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
		File.WriteAllText(GetAbsolutePath(_saveDataPath), JsonConvert.SerializeObject(_assets));
	}

	static void LoadData()
	{
		string jsonText = File.ReadAllText(GetAbsolutePath(_saveDataPath));
		_savedAssets = JsonConvert.DeserializeObject<List<AssetItem>>(jsonText);

		AssetItem[] allSavedItems;
		AssetItem[] allItems;
		allSavedItems = GetAllItems(_savedAssets, true);
		allItems = GetAllItems(_assets, true);
		//Get all saved items

		foreach (AssetItem item in allSavedItems)
		{
			if (ContainsAssetWithTheSamePath(item.path, allItems))
			{
				//Item exist
				GetItemWithTheSamePath(item.path, allItems).enabled = GetItemWithTheSamePath(item.path, allSavedItems).enabled;
			}
			else if (item.tempPath != "")
			{
				cleared = true;
				//Item not exist
			}
		}
	}

	[MenuItem("Resource Manager/Build...")]
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

		string fullPath = PlayerPrefs.GetString("BUILD_PATH", "");
		string buildPath = string.IsNullOrEmpty(fullPath) ? "" : Path.GetDirectoryName(fullPath);
		string appName = Path.GetFileName(fullPath);

		fullPath = EditorUtility.SaveFilePanel("Choose Location of Built Game", buildPath, appName, "");
		
		if (!string.IsNullOrEmpty(fullPath))
		{
			Hide();

			PlayerPrefs.SetString("BUILD_PATH", fullPath);
			PlayerPrefs.Save();
			// Build player
			BuildPipeline.BuildPlayer(scenesPath.ToArray(), fullPath, EditorUserBuildSettings.activeBuildTarget, BuildOptions.None);

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