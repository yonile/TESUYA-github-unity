using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.IO;
using System.Text;
using TotekanReaderLib;

public class TotekanImportProcessor : AssetPostprocessor {

	static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, 
	                                   string[] movedAssets, string[] movedFromAssetPaths)
	{

		if (importedAssets.Length > 0) {
			foreach (var path in DragAndDrop.paths) {
				Debug.Log("Added to import task: " + path);
				var nameFileLowered = path.ToLower();
				if (PlayAnimationImportWindow.getIsAcceptedExtention(Path.GetExtension(nameFileLowered))) {
					PlayAnimationImportWindow.ShowWindow();
					var window = PlayAnimationImportWindow.GetWindow<PlayAnimationImportWindow>();
					window.addTask(nameFileLowered);
				}
			}
			foreach (string nameAsset in importedAssets) {
				var nameAssetLowered = nameAsset.ToLower();

				if (PlayAnimationImportWindow.getIsAcceptedExtention(Path.GetExtension(nameAssetLowered))) {
					AssetDatabase.DeleteAsset(nameAsset);
				} else if (nameAssetLowered.EndsWith(".pic")) {
					Debug.Log("Loading Pic file: " + nameAsset);
					loadPicTexture(nameAsset);
				}
				
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}

	static void loadPicTexture(string nameAssetFullPath)
	{
		var tex = TextureUtil.loadTexture(nameAssetFullPath);

		var nameFolderRoot = "Assets";
		var nameObj = Path.GetFileNameWithoutExtension(nameAssetFullPath);
		AssetDatabase.CreateAsset(tex, nameFolderRoot + "/" + nameObj + ".asset");
		AssetDatabase.DeleteAsset(nameAssetFullPath);
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}
