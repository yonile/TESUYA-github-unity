using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;

public class ImportTask
{
	public enum Status
	{
		NO_PRUDUCTS,
		NEED_TO_SELECT,
		READY,
		IMPORTING,
		FAILED,
		COMPLETED
	};

	public enum ImportStatus
	{
		IDLE				= 0,
		PARSE_PARTS_ASSEMBLER	= 1,
		PARSE_SUFS			= 2,
		CONVERT_MESH		= 3,
		PARSE_ATRS			= 4,
		CONVERT_MATERIAL	= 5,
		BUILD_GAME_OBJECT	= 6,
		COMPLETED			= 7
	}

	public enum ColorL1
	{
		White	= 0,
		Blue	= 1,
		Red		= 2,
		Green	= 3,
		Magenta	= 4
	}

	static public ImportTask.ColorL1[] enumsColorL1 = new ColorL1[] { ColorL1.White, ColorL1.Blue, ColorL1.Red, ColorL1.Green, ColorL1.Magenta };

	private static Dictionary<ImportTask.Status, string> tableMessageStatus;
	private static Dictionary<ImportTask.ImportStatus, string> tableMessageStatusImport;
	private static Dictionary<ImportTask.ColorL1, GUIContent> tableGUIColorL1;

	public string nameAsset;
	public string nameAssetFullPath;
	public float progress = 0.0f;
	public int indexIdProductSelected = 0;
	public int indexColorL1 = 0;
	public string[] idsProductToSelect;
	public string[] namesProductToSelect;
	public Status status;
	public ImportStatus statusImport;

	public ImportTask(string nameAssetFullPath)
	{
		this.nameAsset = Path.GetFileName(nameAssetFullPath);
		this.nameAssetFullPath = nameAssetFullPath;
		this.progress = 0.0f;
		this.status = Status.READY;
		this.statusImport = ImportStatus.IDLE;
	}

	static public string getMessage(Status status)
	{
		if (ImportTask.tableMessageStatus == null) {
			ImportTask.initTableMessageStatus();
		}

		if (!ImportTask.tableMessageStatus.ContainsKey(status)) return "Unknown status: " + status.ToString();
		return ImportTask.tableMessageStatus[status];
	}

	static public string getMessageStatusImport(ImportStatus status)
	{
		if (ImportTask.tableMessageStatusImport == null) {
			ImportTask.initTalbeMessageStatusImport();
		}

		if (!ImportTask.tableMessageStatusImport.ContainsKey(status)) return "Unknown status: " + status.ToString();
		return ImportTask.tableMessageStatusImport[status];
	}

	static public GUIContent getGUIColorL1(ColorL1 color)
	{
		if (ImportTask.tableGUIColorL1 == null) {
			ImportTask.initTableGUIColorL1();
		}
		if (!ImportTask.tableGUIColorL1.ContainsKey(color)) return null;
		return ImportTask.tableGUIColorL1[color];
	}

	static public GUIContent[] getTableGUIColorL1()
	{
		if (ImportTask.tableGUIColorL1 == null) {
			ImportTask.initTableGUIColorL1();
		}
		return ImportTask.tableGUIColorL1.Values.ToArray();
	}

	static private void initTableMessageStatus()
	{
		ImportTask.tableMessageStatus = new Dictionary<ImportTask.Status, string>();
		ImportTask.tableMessageStatus.Add(ImportTask.Status.READY, "Ready to import.");
		ImportTask.tableMessageStatus.Add(ImportTask.Status.IMPORTING, "Importing...");
		ImportTask.tableMessageStatus.Add(ImportTask.Status.NO_PRUDUCTS, "No DoGA products installed for this file.");
		ImportTask.tableMessageStatus.Add(ImportTask.Status.NEED_TO_SELECT, "Please select DoGA product.");
		ImportTask.tableMessageStatus.Add(ImportTask.Status.FAILED, "Import failed. Please see console log.");
		ImportTask.tableMessageStatus.Add(ImportTask.Status.COMPLETED, "Completed.");
	}

	static private void initTalbeMessageStatusImport()
	{
		ImportTask.tableMessageStatusImport = new Dictionary<ImportTask.ImportStatus, string>();
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.IDLE, "Nothing to do.");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.PARSE_PARTS_ASSEMBLER, "Parsing parts assermbler file...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.PARSE_SUFS, "Parsing sufs...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.CONVERT_MESH, "Converting to meshes...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.PARSE_ATRS, "Parsing atrs...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.CONVERT_MATERIAL, "Converting materials...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.BUILD_GAME_OBJECT, "Building game object...");
		ImportTask.tableMessageStatusImport.Add(ImportTask.ImportStatus.COMPLETED, "Completed.");
	}

	static private void initTableGUIColorL1()
	{
		var colorsL1 = new Color32[] { new Color32(255, 255, 255, 255), new Color32(128, 128, 255, 255),
			new Color32(255, 128, 128, 255), new Color32(128, 255, 128, 255), new Color32(255, 128, 255, 255) };

		ImportTask.tableGUIColorL1 = new Dictionary<ColorL1, GUIContent>();
		int widthIcon = 32, heightIcon = 32;

		for (var index = 0; index < ImportTask.enumsColorL1.Length; index++) {
			var texture = new Texture2D(widthIcon, heightIcon, TextureFormat.ARGB32, false);
			var buffer = new Color32[widthIcon * heightIcon];
			for (var indexBuffer = 0; indexBuffer < widthIcon * heightIcon; indexBuffer++) {
				buffer[indexBuffer] = colorsL1[index];
			}
			texture.SetPixels32(buffer);
			texture.Apply();
			ImportTask.tableGUIColorL1.Add(ImportTask.enumsColorL1[index], new GUIContent(ImportTask.enumsColorL1[index].ToString(), texture));

		}
	}
}

