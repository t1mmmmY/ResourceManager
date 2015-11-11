using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace CustomBuildManager
{

	public static class BuildSettings
	{
		//Set Bundle Id here
		public static readonly Dictionary<BuildTargetPlatform, string> bundleId = new Dictionary<BuildTargetPlatform, string>()
		{
			{ BuildTargetPlatform.iOS, "com.ios.id" },
			{ BuildTargetPlatform.Android, "com.android.id" },
			{ BuildTargetPlatform.Amazon, "com.amazon.id" }
		};

		//Set Bundle version here
		public static readonly Dictionary<BuildTargetPlatform, string> bundleVersion = new Dictionary<BuildTargetPlatform, string>()
		{
			{ BuildTargetPlatform.iOS, "1.0.3" },
			{ BuildTargetPlatform.Android, "1.0.3" },
			{ BuildTargetPlatform.Amazon, "1.0.3" }
		};

		//Set Bundle version code here
		public static readonly Dictionary<BuildTargetPlatform, int> bundleVersionCode = new Dictionary<BuildTargetPlatform, int>()
		{
			{ BuildTargetPlatform.iOS, 1 },
			{ BuildTargetPlatform.Android, 1 },
			{ BuildTargetPlatform.Amazon, 1 }
		};

		//Set Split application here
		public static readonly Dictionary<BuildTargetPlatform, bool> splitApplication = new Dictionary<BuildTargetPlatform, bool>()
		{
			{ BuildTargetPlatform.iOS, false },
			{ BuildTargetPlatform.Android, true },
			{ BuildTargetPlatform.Amazon, false }
		};

		//Should not change this
		public static readonly Dictionary<BuildTargetPlatform, BuildTarget> buildTarget = new Dictionary<BuildTargetPlatform, BuildTarget>()
		{
			{ BuildTargetPlatform.iOS, BuildTarget.iOS },
			{ BuildTargetPlatform.Android, BuildTarget.Android },
			{ BuildTargetPlatform.Amazon, BuildTarget.Android }
		};
	}

	public enum BuildTargetPlatform
	{
		iOS,
		Android,
		Amazon
	}



	public class BuildManager 
	{
		public static System.Action onStartBuilding;
		public static System.Action onFinishBuilding;

		private static void BuildGame(BuildTargetPlatform targetPlatform)
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
				if (onStartBuilding != null)
				{
					onStartBuilding();
				}

				//Hide unused assets
				ResourceManager.Hide();

				
				PlayerPrefs.SetString("BUILD_PATH", fullPath);
				PlayerPrefs.Save();

	#region Save old settings

				string oldBundleId = PlayerSettings.bundleIdentifier;
				string oldBundleVersion = PlayerSettings.bundleVersion;
				int oldBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
				bool oldSplitApplication = PlayerSettings.Android.useAPKExpansionFiles;

	#endregion


	#region Change settings
				
				PlayerSettings.bundleIdentifier = BuildSettings.bundleId[targetPlatform];
				PlayerSettings.bundleVersion = BuildSettings.bundleVersion[targetPlatform];
				PlayerSettings.Android.bundleVersionCode = BuildSettings.bundleVersionCode[targetPlatform];
				PlayerSettings.Android.useAPKExpansionFiles = BuildSettings.splitApplication[targetPlatform];

	#endregion

				// Build player
				BuildPipeline.BuildPlayer(scenesPath.ToArray(), fullPath, BuildSettings.buildTarget[targetPlatform], BuildOptions.None);

				//Rename obb file
				if (BuildSettings.splitApplication[targetPlatform])
				{
					FileInfo apkInfo = new FileInfo(fullPath);
					string obbFilePath = fullPath.Replace(apkInfo.Extension, ".main.obb");

					FileInfo obbInfo = new FileInfo(obbFilePath);

					string obbNewPath = string.Format("{0}/main.{1}.{2}.obb", obbInfo.Directory.FullName, 
					                                  BuildSettings.bundleVersionCode[targetPlatform], 
					                                  BuildSettings.bundleId[targetPlatform]);

					File.Move(obbFilePath, obbNewPath);
				}

	#region Restore old settings
				
				PlayerSettings.bundleIdentifier = oldBundleId;
				PlayerSettings.bundleVersion = oldBundleVersion;
				PlayerSettings.Android.bundleVersionCode = oldBundleVersionCode;
				PlayerSettings.Android.useAPKExpansionFiles = oldSplitApplication;

	#endregion

				//Restore unused assets
				ResourceManager.Restore();


				if (onFinishBuilding != null)
				{
					onFinishBuilding();
				}
			}
		}



		[MenuItem("Resource Manager/Build iOS...")]
		private static void BuildIOS()
		{
			BuildGame(BuildTargetPlatform.iOS);
		}

		[MenuItem("Resource Manager/Build Android...")]
		private static void BuildAndroid()
		{
			BuildGame(BuildTargetPlatform.Android);
		}

		[MenuItem("Resource Manager/Build Amazon...")]
		private static void BuildAmazon()
		{
			BuildGame(BuildTargetPlatform.Amazon);
		}

	}

}
