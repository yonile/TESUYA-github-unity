using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using TotekanReaderLib;
using System.IO;

public class PlayAnimationImportWindow : EditorWindow
{
	static public string[] namesExtentionToBeAccepted = new string[] { ".e1p", ".l3p", ".l2p", ".fsc" };
	private DetectInstall detectedInstall;

	private List<ImportTask> tasks = new List<ImportTask>();

	private Vector2 paramScroll = new Vector2();

	private ImportWorker worker;
	private IEnumerator<bool> workerEnumerable;

	private Vector3 scaleImport = new Vector3(1.0f, 1.0f, 1.0f);
	private Vector3 scaleImportInit = new Vector3(0.005f, 0.005f, 0.005f);

	private bool showProductsInstalled = true;
	private bool showOptionImport = true;
	private bool showTasksImport = true;

	public PlayAnimationImportWindow()
	{
		this.detectInstall();	
	}

	[MenuItem("Window/PLAY Animation (TotekanCG) Importer")]
	static void Init()
	{
		var window = PlayAnimationImportWindow.GetWindow<PlayAnimationImportWindow>();
		window.detectInstall();
	}

	
	static public bool getIsAcceptedExtention(string extension)
	{
		return namesExtentionToBeAccepted.Contains(extension);
	}

	public void detectInstall()
	{
		if (this.detectedInstall == null) {
			this.detectedInstall = new DetectInstall();
			this.worker = new ImportWorker(this.detectedInstall);
		}
	}

	public void addTask(string nameAsset)
	{
		var nameExt = Path.GetExtension(nameAsset).ToLower();
		var idsProduct = this.detectedInstall.getIdsProductByExtPartAsm(nameExt);
		var task = new ImportTask(nameAsset);
		task.idsProductToSelect = idsProduct.ToArray();
		var namesProduct = new List<string>();
		foreach (var idProduct in task.idsProductToSelect) {
			var nameProduct = this.detectedInstall.tableInfosInstall[idProduct].nameProduct;
			namesProduct.Add(nameProduct);
		}
		task.namesProductToSelect = namesProduct.ToArray();

		if (idsProduct.Count == 0) {
			task.status = ImportTask.Status.NO_PRUDUCTS;
		} else {
				task.status = ImportTask.Status.READY;
		}
		this.tasks.Add(task);
	}

	public void OnGUI()
	{
		if (this.detectedInstall == null) return;

		float spaceHorizontal = 5.0f;

		var namesProduct = new List<string>();
		foreach (var idProduct in this.detectedInstall.tableInfosInstall.Keys) {
			namesProduct.Add(this.detectedInstall.tableInfosInstall[idProduct].nameProduct);
		}

		this.showProductsInstalled = EditorGUILayout.Foldout(this.showProductsInstalled, "Installed DoGA products");
		if (this.showProductsInstalled) {
			EditorGUILayout.LabelField(string.Join(",", namesProduct.ToArray()));
			EditorGUILayout.Space();
		}

		this.showOptionImport = EditorGUILayout.Foldout(this.showOptionImport, "Import options");
		if (this.showOptionImport) {
			EditorGUILayout.BeginHorizontal();
			this.scaleImport = EditorGUILayout.Vector3Field("Scale", this.scaleImport, GUILayout.Width(Screen.width * 0.8f - spaceHorizontal));
			EditorGUILayout.BeginVertical();
			GUILayout.Space(16.0f);
			if (GUILayout.Button("Reset", GUILayout.Width(Screen.width * 0.2f - spaceHorizontal))) {
				this.scaleImport.Set(this.scaleImportInit.x, this.scaleImportInit.y, this.scaleImportInit.z);
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}


		if (this.workerEnumerable != null) {
			this.workerEnumerable.MoveNext();
			this.Repaint();
			if (this.workerEnumerable.Current == true) {
				this.workerEnumerable.Dispose();
				this.workerEnumerable = null;
				GUI.enabled = true;
			}
		}

		this.showTasksImport = EditorGUILayout.Foldout(this.showTasksImport, "Import tasks");
		if (this.showTasksImport) {
			this.paramScroll = EditorGUILayout.BeginScrollView(this.paramScroll);
			foreach (var task in this.tasks) {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.SelectableLabel(task.nameAsset, EditorStyles.textField, GUILayout.Width(Screen.width * 0.25f - spaceHorizontal), GUILayout.Height(EditorGUIUtility.singleLineHeight));

				var isL1 = false;
				if (task.indexIdProductSelected < task.namesProductToSelect.Length - 1) {
					isL1 = (task.namesProductToSelect[task.indexIdProductSelected] == "DOGA-L1") ? true : false;
				}
 				var ratioWidthProduct = isL1 ? 0.15f : 0.25f;
				var indexIdProduct = EditorGUILayout.Popup(task.indexIdProductSelected, task.namesProductToSelect, GUILayout.Width(Screen.width * ratioWidthProduct - spaceHorizontal));
				if (indexIdProduct != task.indexIdProductSelected) {
					task.indexIdProductSelected = indexIdProduct;
					task.status = ImportTask.Status.READY;
				} 
				if (isL1) {
					task.indexColorL1 = EditorGUILayout.Popup(task.indexColorL1, ImportTask.getTableGUIColorL1(), GUILayout.Width(Screen.width * 0.10f - spaceHorizontal));
				}
				var rectH = EditorGUILayout.BeginHorizontal(GUILayout.Width(Screen.width * 0.5f - spaceHorizontal));
				var rect = new Rect(rectH.xMin, rectH.yMin + 1.0f, rectH.width, EditorGUIUtility.singleLineHeight);
				var ratioProgress = (float)task.statusImport / (float)ImportTask.ImportStatus.COMPLETED;
				var builderMessage = new StringBuilder();
				builderMessage.Append(ImportTask.getMessage(task.status));
				if (task.status == ImportTask.Status.IMPORTING) {
					builderMessage.Append("(");
					builderMessage.Append(ImportTask.getMessageStatusImport(task.statusImport));
					builderMessage.Append(")");
				}
				EditorGUI.ProgressBar(rect, ratioProgress, builderMessage.ToString());
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
			
				EditorGUILayout.EndHorizontal();
			}

			var evt = Event.current;
			var dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
			var style = new GUIStyle();
			style.alignment = TextAnchor.MiddleCenter;
			GUI.Box(dropArea, "(Drag & drop here)", style);
			switch (evt.type) {
				case EventType.DragUpdated:
				case EventType.DragPerform:
					if (!dropArea.Contains(evt.mousePosition)) break;
					bool doesContainPathAccepted = false;
					foreach (var path in DragAndDrop.paths) {
						if (getIsAcceptedExtention(Path.GetExtension(path).ToLower())) {
							doesContainPathAccepted = true;
						}
					}
					DragAndDrop.visualMode = doesContainPathAccepted ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

					if (evt.type == EventType.DragPerform) {
						DragAndDrop.AcceptDrag();
						foreach (var path in DragAndDrop.paths) {
							if (getIsAcceptedExtention(Path.GetExtension(path).ToLower())) {
								this.addTask(path);
							}
						}
						DragAndDrop.activeControlID = 0;
					}
					Event.current.Use();
					break;
			}


			EditorGUILayout.EndScrollView();
		}


		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Import All", GUILayout.Width(Screen.width * 0.5f - spaceHorizontal))) {
			this.workerEnumerable = this.worker.importPartsAsm(this.tasks, this.scaleImport);
			GUI.enabled = false;
		}
		if (GUILayout.Button("Clear", GUILayout.Width(Screen.width * 0.25f - spaceHorizontal))) {
			this.tasks.Clear();
		}
		if (GUILayout.Button("Close", GUILayout.Width(Screen.width * 0.25f - spaceHorizontal))) {
			var window = EditorWindow.GetWindow(typeof(PlayAnimationImportWindow));
			window.Close();

		}
		EditorGUILayout.EndHorizontal();
	}

	static public void ShowWindow()
	{
		var window = EditorWindow.GetWindow(typeof(PlayAnimationImportWindow));
		window.ShowUtility();	
	}

	private void OnEnable()
	{
		this.scaleImport.x = EditorPrefs.GetFloat("DoGA_PLAYAnimation_ScaleImport_X", this.scaleImportInit.x);
		this.scaleImport.y = EditorPrefs.GetFloat("DoGA_PLAYAnimation_ScaleImport_Y", this.scaleImportInit.z);
		this.scaleImport.z = EditorPrefs.GetFloat("DoGA_PLAYAnimation_ScaleImport_Z", this.scaleImportInit.y);
	}
	private void OnDisable()
	{
		EditorPrefs.SetFloat("DoGA_PLAYAnimation_ScaleImport_X", this.scaleImport.x);
		EditorPrefs.SetFloat("DoGA_PLAYAnimation_ScaleImport_Y", this.scaleImport.y);
		EditorPrefs.SetFloat("DoGA_PLAYAnimation_ScaleImport_Z", this.scaleImport.z);
	}
}
