using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;

using UnityEngine;
using UnityEditor;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using TotekanReaderLib;

public class ImportWorker
{
	private DetectInstall detectedInstall;
	private Dictionary<string, DoGAAtrInfo> tableInfosAtr = new Dictionary<string,DoGAAtrInfo>();
	private Dictionary<string, Texture2D> tableTextures = new Dictionary<string,Texture2D>();

	public ImportWorker(DetectInstall detectedInstall)
	{
		this.detectedInstall = detectedInstall;
	}

	public IEnumerator<bool> importPartsAsm(List<ImportTask> tasks, Vector3 scaleImport)
	{

		foreach (var task in tasks) {
			if (task.status != ImportTask.Status.READY) continue;

			task.status = ImportTask.Status.IMPORTING;
			task.statusImport = ImportTask.ImportStatus.PARSE_PARTS_ASSEMBLER;
			yield return false;

			var idProduct = task.idsProductToSelect[task.indexIdProductSelected];
			var infoInstall = this.detectedInstall.tableInfosInstall[idProduct];
			var pathDirParts = infoInstall.pathInstalled + infoInstall.pathRelativeExe + infoInstall.profiles["PartsDir"] + @"\";
			var pathFileAtr = infoInstall.pathInstalled + infoInstall.pathRelativeExe + infoInstall.profiles["AtrFile"];
			var pathDirAtr = Path.GetDirectoryName(pathFileAtr) + @"\";
			var pathDirData = infoInstall.pathInstalled + @"\data\";
			var pathPartsAssembler = Path.GetDirectoryName(task.nameAssetFullPath) + Path.DirectorySeparatorChar;

			var context = TotekanParse.parseE1P(task.nameAssetFullPath);
			DoGAE1PVisitor visitor = new DoGAE1PVisitor(pathPartsAssembler);
			visitor.Visit(context as IParseTree);


			var nameE1P = Path.GetFileNameWithoutExtension(task.nameAssetFullPath);
			string nameE1PUnique = "";
			if (!AssetNameUtil.getNameAssetUtil("Assets", nameE1P, @".prefab", out nameE1PUnique, 1000)) {
				Debug.Log("Failed to find alternative name for existing file/dir for \"" + nameE1P + "\".");
				task.status = ImportTask.Status.FAILED;
				yield return true;
			} else {
				nameE1P = nameE1PUnique;
			}

			var guidFolder = AssetDatabase.CreateFolder("Assets", nameE1P);
			var pathAsset = AssetDatabase.GUIDToAssetPath(guidFolder) + @"/";

			task.statusImport = ImportTask.ImportStatus.PARSE_SUFS;
			yield return false;

			var pathsParts = new Dictionary<string, bool>(); // path, need to split relative dir
			pathsParts.Add("", false); // use absolute dir
			pathsParts.Add(pathPartsAssembler, true);
			pathsParts.Add(pathDirParts, false);
			pathsParts.Add(pathDirData, false);

			var listSufObjs = visitor.listSufObjs;
			var mapNamesFileSuf = getMapNamesFileSuf(pathsParts, listSufObjs);
			var mapSufs = importSufs(mapNamesFileSuf);
			var mapAtrsSufDir = importAtrsSufDir(mapNamesFileSuf);

			task.statusImport = ImportTask.ImportStatus.CONVERT_MESH;
			yield return false;

			var mapMeshSuf = new Dictionary<string, Pair<Mesh, List<Pair<string, bool>>>>();
			foreach (var pair in mapSufs) {
				var nameFileSuf = pair.Key;
				var listSufs = pair.Value;

				var infoMesh = new TotekanMeshInfo();
				buildSufMesh(listSufs, ref infoMesh);
				var mesh = createMesh(infoMesh);
				mapMeshSuf.Add(nameFileSuf, new Pair<Mesh, List<Pair<string, bool>>>(mesh, infoMesh.namesAtr));
			}

			task.statusImport = ImportTask.ImportStatus.PARSE_ATRS;
			yield return false;

			DoGAAtrInfo infoAtr = null;
			if (idProduct != "DOGA-L1") {
				infoAtr = this.getInfoAtrWithLoad(idProduct);
			}

			Dictionary<string, DoGAAtr> atrsColorL1 = null;
			if (idProduct == "DOGA-L1") {
				atrsColorL1 = this.loadAtrsColorL1(pathDirAtr, ImportTask.enumsColorL1[task.indexColorL1]);
			}

			// Create game object
			var objectGameRoot = new GameObject(nameE1P);
			objectGameRoot.transform.rotation = Quaternion.AngleAxis(-120.0f, new Vector3(1.0f, 1.0f, 1.0f));
			objectGameRoot.transform.localScale = scaleImport;

			task.statusImport = ImportTask.ImportStatus.CONVERT_MATERIAL;
			yield return false;

			var mapAtrsPalette = visitor.mapAtrsPalette; // atrs in PartsAssembler (L3)

			// Create materials from atr
			var mapAtrs = new Dictionary<string, Material>();
			var setNamesFileImageTextureNotFound = new HashSet<string>();
			foreach (var sufobj in listSufObjs) {
				var nameFileSuf = sufobj.nameFileSuf;
				// Debug.Log(" sufobj.nameFileSuf: " +  sufobj.nameFileSuf + ", nameFileSuf: " + nameFileSuf);
				var nameAssetModel = getNameFileAsset(nameFileSuf);

				if (!mapMeshSuf.ContainsKey(nameFileSuf)) {
					continue;
				}
				var namesAtrInSuf = mapMeshSuf[nameFileSuf].Second;
				foreach (var nameAtrInSuf in namesAtrInSuf) {
					var nameAtrAsset = getNameAtrAsset(nameAssetModel, sufobj.nameAtrColor, sufobj.nameTexture, sufobj.nameMaterial, nameAtrInSuf);
					if (mapAtrs.ContainsKey(nameAtrAsset)) continue;
					//				Debug.Log("nameAtrInSuf: " + nameAtrAsset);

					DoGAAtr atr = null;
					// retrieve atr from atr placed atr the suf dir..
					var nameFileAtr = Path.GetFileNameWithoutExtension(mapNamesFileSuf[nameFileSuf]) + @".atr";
					if (atr == null && mapAtrsSufDir.ContainsKey(nameFileAtr)) {
						var mapAtrSuf = mapAtrsSufDir[nameFileAtr];
						if (mapAtrSuf.ContainsKey(nameAtrInSuf.First)) {
							atr = mapAtrSuf[nameAtrInSuf.First];
						} 
					}
					// retrieve atr from global palette
					if (atr == null) {
						if (infoAtr != null) {
							try {
								atr = infoAtr.getAtrFromPalette(nameAtrInSuf.First, sufobj.nameAtrColor, sufobj.nameTexture, sufobj.nameMaterial, mapAtrsPalette);
							} catch (KeyNotFoundException ex) {
								Debug.Log("Cannot found atr palette: " +
									"Caught exception: " + ex.Message + ", " + 
									"nameAtrInSuf=" + nameAtrInSuf.First + ", " +
									"nameAtrColor=" + sufobj.nameAtrColor + ", " +
									"nameAtrTexture=" + sufobj.nameTexture + ", " +
									"nameAtrMaterial=" + sufobj.nameMaterial);
							}
						}
						if (atrsColorL1 != null) {
							if (atrsColorL1.ContainsKey(nameAtrInSuf.First)) {
								atr = atrsColorL1[nameAtrInSuf.First];
							}
						}
					}

					if (atr != null) {
						Material material = null;
						var textures = new Dictionary<string, Texture2D>();
						try {
							if (nameAtrInSuf.Second) {
								foreach (var nameKey in atr.textures.Keys) {
									if (nameKey != "colormap") continue;
									var namesFileTexture = getNamesFileTexture(atr.textures[nameKey]);
									var namesFileTextureFull = getPathTexture(namesFileTexture, atr.pathsTexture);
									var nameKeyTexture = String.Join(@"_", namesFileTextureFull.ToArray());

									Debug.Log("Texture: ");
									Texture2D texture = null;
									if (!this.tableTextures.ContainsKey(nameKeyTexture)) {
										var textureOrg = TextureUtil.loadAndBlendTextures(namesFileTextureFull);
										if (textureOrg != null) {
											var nameFileTextureAsset = getNameFilesTextureAsset(namesFileTexture);
											// Save loaded and blended texture to asset folder as PNG format
											var pathTexturePng = pathAsset + nameFileTextureAsset + "_texture.png";
											File.WriteAllBytes(pathTexturePng, textureOrg.EncodeToPNG());
											// Then import and use it.
											AssetDatabase.ImportAsset(pathTexturePng);
											texture = AssetDatabase.LoadAssetAtPath(pathTexturePng, typeof(Texture2D)) as Texture2D;
											this.tableTextures.Add(nameKeyTexture, texture);
										} else {
											foreach (var nameFileTextureFull in namesFileTextureFull) {
												if (setNamesFileImageTextureNotFound.Contains(nameFileTextureFull)) continue;
												Debug.Log("Could not find texture image: " + nameFileTextureFull);
												setNamesFileImageTextureNotFound.Add(nameFileTextureFull);
											}
										}
									} else {
										texture = this.tableTextures[nameKeyTexture];
									}
									if (texture != null) {
										textures.Add(nameKey, texture);
									}
								}
							}
						} catch (Exception ex) {
							Debug.Log("Caught exception for creating textures: " +
								ex.Message + ", " +
								"nameAtrInSuf=" + nameAtrInSuf.First + ", " +
								"nameAtrColor=" + sufobj.nameAtrColor + ", " +
								"nameAtrTexture=" + sufobj.nameTexture + ", " +
								"nameAtrMaterial=" + sufobj.nameMaterial);
						}
						try {
							// if atr contains transparent texture, material is also transparent.
							bool isMaterialTransparent = false;
							foreach (var texture in textures.Values) {
								if (texture.alphaIsTransparency) isMaterialTransparent = true;
							}
							var color = atr.getColor("col");
							if (color != null) {
								material = new Material(Shader.Find(isMaterialTransparent ? "Transparent/Diffuse" : "Diffuse"));
								material.color = new Color(color.r, color.g, color.b);

								// Set texture to materal.
								foreach (var pairTexture in textures) {
									if (pairTexture.Key == "colormap") {
										material.mainTexture = pairTexture.Value;
										material.mainTextureOffset = Vector2.zero;
										material.mainTextureScale = new Vector2(1.0f, -1.0f);
									} if (pairTexture.Key == "bumpmap") {
										//set bump texture to material
									}
								}
							}
						} catch (Exception ex) {
							Debug.Log("Caught exception in attaching texture to material: " +
								ex.Message + ", " +
								"nameAtrInSuf=" + nameAtrInSuf.First + ", " +
								"nameAtrColor=" + sufobj.nameAtrColor + ", " +
								"nameAtrTexture=" + sufobj.nameTexture + ", " +
								"nameAtrMaterial=" + sufobj.nameMaterial);
						}
						if (material != null) {
							AssetDatabase.CreateAsset(material, pathAsset + nameAtrAsset + "_material.asset");
							mapAtrs.Add(nameAtrAsset, material);
						}
					}
				}
			}

			task.statusImport = ImportTask.ImportStatus.BUILD_GAME_OBJECT;
			yield return false;

			var mapCombines = new Dictionary<string, List<CombineInstance>>(); // atr name to combine instances
			foreach (var nameAtr in mapAtrs.Keys) {
				mapCombines.Add(nameAtr, new List<CombineInstance>());
			}

			int indexSufObj = 0;
			foreach (var sufobj in listSufObjs) {
				Vector3 pos;
				Quaternion quat;
				Vector3 scal;
				getMatrixTransform(sufobj.listTransforms, out pos, out quat, out scal);
				var nameFileSuf = sufobj.nameFileSuf;
				if (!mapMeshSuf.ContainsKey(nameFileSuf)) {
					continue;
				}
				var mesh = mapMeshSuf[nameFileSuf].First;
				var namesAtr = mapMeshSuf[nameFileSuf].Second;

				var nameAssetModel = getNameFileAsset(nameFileSuf);

				var transformLocal = new Matrix4x4();
				transformLocal.SetTRS(pos, quat, scal);
				var transformWorld = objectGameRoot.transform.localToWorldMatrix * transformLocal;

				for (int index = 0; index < mesh.subMeshCount; index++) {
					var nameAtrInSuf = namesAtr[index];
					var nameAtrAsset = getNameAtrAsset(nameAssetModel, sufobj.nameAtrColor, sufobj.nameTexture, sufobj.nameMaterial, nameAtrInSuf);
					if (!mapAtrs.ContainsKey(nameAtrAsset)) continue;

					var combine = new CombineInstance();
					combine.mesh = mesh;
					combine.subMeshIndex = index;
					combine.transform = transformWorld;
					mapCombines[nameAtrAsset].Add(combine);
				}
				indexSufObj++;
			}

			int indexMaterial = 0;
			foreach (var pairCombine in mapCombines) {
				if (pairCombine.Value.Count == 0) continue;
				var nameAtrAsset = pairCombine.Key;
				var mesh = new Mesh();
				mesh.CombineMeshes(pairCombine.Value.ToArray(), true, true);
				AssetDatabase.CreateAsset(mesh, pathAsset + nameAtrAsset + "_model.asset");
				var objectGame = new GameObject(indexMaterial.ToString() + "_" + nameAtrAsset);
				objectGame.transform.parent = objectGameRoot.transform;
				var renderer = objectGame.AddComponent<MeshRenderer>() as MeshRenderer;
				var materials = new Material[] { mapAtrs[nameAtrAsset] };
				renderer.materials = materials;
				var filterMesh = objectGame.AddComponent<MeshFilter>();
				filterMesh.mesh = mesh;
				indexMaterial++;
			}

			AssetDatabase.DeleteAsset(task.nameAssetFullPath);
			AssetDatabase.SaveAssets();
			Debug.Log("Saved all assets");

			var pathPrefab = "Assets/" + nameE1P + ".prefab";
			var pathPrefabUnique = AssetDatabase.GenerateUniqueAssetPath(pathPrefab);
			PrefabUtility.CreatePrefab(pathPrefabUnique, objectGameRoot, ReplacePrefabOptions.ConnectToPrefab);

			task.statusImport = ImportTask.ImportStatus.COMPLETED;
			task.status = ImportTask.Status.COMPLETED;
			yield return false;
		}

		this.tableInfosAtr.Clear();
		this.tableTextures.Clear();

		yield return true;
	}

	private DoGAAtrInfo getInfoAtrWithLoad(string idProduct)
	{
		if (!this.tableInfosAtr.ContainsKey(idProduct)) {
			var infoInstall = this.detectedInstall.tableInfosInstall[idProduct];
			var infoAtr = new DoGAAtrInfo(idProduct, infoInstall.pathInstalled + infoInstall.pathRelativeExe, infoInstall.profiles);
			this.tableInfosAtr.Add(idProduct, infoAtr);
			return infoAtr;
		}
		return this.tableInfosAtr[idProduct];
	}

	static string getNameFileAsset(string nameFile)
	{
		if (Path.IsPathRooted(nameFile)) {
			return Path.GetFileNameWithoutExtension(nameFile);
		} else {
			var pathDir = Path.GetDirectoryName(nameFile).Replace(@"/", @"_").Replace(@"\", @"_");
			var pathDirAsset = "";
			if (pathDir.Length > 0) {
				pathDirAsset = pathDir.Replace(@"/", @"_").Replace(@"\", @"_") + @"_";
			}
			return pathDirAsset + Path.GetFileNameWithoutExtension(nameFile);
		}
	}

	static string getNameFilesTextureAsset(List<string> namesFileTexture)
	{
		var namesFileWithoutExt = new List<string>();
		foreach (var nameFile in namesFileTexture) {
			var builder = new StringBuilder();
			var path = Path.GetDirectoryName(nameFile);
			if (path.Length > 0) {
				builder.Append(path);
				builder.Append(Path.DirectorySeparatorChar);
			}
			builder.Append(Path.GetFileNameWithoutExtension(nameFile));
			namesFileWithoutExt.Add(builder.ToString());
		}
		return getNameFileAsset(String.Join(@"_", namesFileWithoutExt.ToArray()));
	}

	static string getNameAtrAsset(string nameAssetModel, string nameAtrColor, string nameAtrTexture, string nameAtrMaterial, Pair<string, bool> nameAtrInSuf)
	{
//		return nameAssetModel + "_" + nameAtrColor + "_" + nameAtrTexture + "_" + nameAtrMaterial + "_" + nameAtrInSuf.First + "_" + (nameAtrInSuf.Second ? "uv" : "nouv");
		return nameAtrColor + "_" + nameAtrTexture + "_" + nameAtrMaterial + "_" + nameAtrInSuf.First + "_" + (nameAtrInSuf.Second ? "uv" : "nouv");
	}

	static List<string> getNamesFileTexture(List<TextureInfo> infosTexture)
	{
		var namesFileTexture = new List<string>();
		foreach (var infoTexture in infosTexture) {
			namesFileTexture.Add(infoTexture.pathTexture);
		}
		return namesFileTexture;
	}

	static List<string> getPathTexture(List<string> namesFileTexture, List<string> paths)
	{
		var pathsTexture = new List<string>();
		foreach (var nameFileTexture in namesFileTexture) {
			if (Path.IsPathRooted(nameFileTexture)) {
				pathsTexture.Add(nameFileTexture);
				continue;
			}
			foreach (var path in paths) {
				var nameFileTextureFull = path + Path.DirectorySeparatorChar + nameFileTexture;
				if (File.Exists(nameFileTextureFull)) {
					pathsTexture.Add(nameFileTextureFull);
					break;
				}
			}
		}
		return pathsTexture;
	}

	static Mesh createMesh(TotekanMeshInfo infoMesh)
	{
		var mesh = new Mesh();
		mesh.vertices = infoMesh.positions.ToArray();
		mesh.normals = infoMesh.normals.ToArray();
		mesh.uv = infoMesh.uvs.ToArray();
		mesh.subMeshCount = infoMesh.indices.Count;
		int indexSubMesh = 0;
		foreach (var indices in infoMesh.indices) {
			mesh.SetTriangles(indices.ToArray(), indexSubMesh);
			indexSubMesh++;
		}

		return mesh;
	}

	static Dictionary<string, string> getMapNamesFileSuf(Dictionary<string, bool> paths, List<DoGASufInfo> infosSuf)
	{
		var mapNamesSuf = new Dictionary<string, string>();
		var setNamesSufNotFound = new HashSet<string>();
		foreach (var infoSuf in infosSuf) {
			if (mapNamesSuf.ContainsKey(infoSuf.nameFileSuf)) continue;
			bool isFound = false;
			foreach (var path in paths) {
				var nameFileRelative = path.Value ? Path.GetFileName(infoSuf.nameFileSuf) : infoSuf.nameFileSuf;
				var nameFileSufFullPath = path.Key + nameFileRelative;
				if (File.Exists(nameFileSufFullPath)) {
					mapNamesSuf.Add(infoSuf.nameFileSuf, nameFileSufFullPath);
					isFound = true;
					break;
				}
			}
			if (!isFound && !setNamesSufNotFound.Contains(infoSuf.nameFileSuf)) {
				setNamesSufNotFound.Add(infoSuf.nameFileSuf);
				Debug.Log("Could not find suf: " + infoSuf.nameFileSuf);
			}			
		}
		return mapNamesSuf;
	}

	static Dictionary<string, List<DoGASuf>> importSufs(Dictionary<string, string> mapNamesSuf)
	{
		var mapSufs = new Dictionary<string, List<DoGASuf>>();
		foreach (var pairNameFileSuf in mapNamesSuf) {
			var suf = importSuf(pairNameFileSuf.Value);
			mapSufs.Add(pairNameFileSuf.Key, suf);
		}
		return mapSufs;
	}

	static Dictionary<string, Dictionary<string, DoGAAtr>> importAtrsSufDir(Dictionary<string, string> mapNamesSuf)
	{
		var mapNamesFileAtrFullPath = new Dictionary<string, string>();
		foreach (var pairNameFileSuf in mapNamesSuf) {
			var nameFileSuf = pairNameFileSuf.Key;
			var nameFileSufFullPath = pairNameFileSuf.Value;
			var nameFileAtr = Path.GetFileNameWithoutExtension(nameFileSuf) + ".atr";
			var nameFileAtrFullPath = Path.GetDirectoryName(nameFileSufFullPath) + @"\"+ nameFileAtr;
			mapNamesFileAtrFullPath.Add(nameFileAtr, nameFileAtrFullPath);
		}
		var mapAtrsPerFile = DoGAAtrInfo.loadFilesAtr(mapNamesFileAtrFullPath);
		return mapAtrsPerFile;
	}

	public Dictionary<string, DoGAAtr> loadAtrsColorL1(string pathDirAtr, ImportTask.ColorL1 color)
	{
		var tablesNameFileAtrColorL1 = new Dictionary<ImportTask.ColorL1, string>();
		tablesNameFileAtrColorL1.Add(ImportTask.ColorL1.White, "GENIE_WH.ATR");
		tablesNameFileAtrColorL1.Add(ImportTask.ColorL1.Blue, "GENIE_BL.ATR");
		tablesNameFileAtrColorL1.Add(ImportTask.ColorL1.Red, "GENIE_RD.ATR");
		tablesNameFileAtrColorL1.Add(ImportTask.ColorL1.Green, "GENIE_GR.ATR");
		tablesNameFileAtrColorL1.Add(ImportTask.ColorL1.Magenta, "GENIE_MG.ATR");

		if (!tablesNameFileAtrColorL1.ContainsKey(color)) return null;
		var nameFileAtr = tablesNameFileAtrColorL1[color];
 		var setNamesFileAtr = new HashSet<string>();
		setNamesFileAtr.Add(nameFileAtr);
		var mapAtrs = DoGAAtrInfo.loadFilesAtr(pathDirAtr, setNamesFileAtr);

		return mapAtrs[nameFileAtr];
	}

	static void outputDebug(TotekanMeshInfo infoMesh)
	{
		var strPos = getStrList<Vector3>(infoMesh.positions);
		Debug.Log("MeshInfo: pos" + strPos);
		var strNorm = getStrList<Vector3>(infoMesh.normals);
		Debug.Log("MeshInfo: normal" + strNorm);
		var strUVs = getStrList<Vector2>(infoMesh.uvs);
		Debug.Log("MeshInfo: uv" + strUVs);

		int indexTriangle = 0;
		var builder = new StringBuilder();
		foreach (var indicesTriangle in infoMesh.indices) {
			builder.Append(indexTriangle + ":(");
			foreach (var index in indicesTriangle) {
				builder.Append(index + " ");
			}
			builder.Append(") ");
			indexTriangle++;
		}
		Debug.Log("MeshInfo: indices: " + builder.ToString());
	}

	static string getStrList<T>(List<T> listVectors)
	{
		var builder = new StringBuilder();
		var index = 0;
		foreach (var vec in listVectors) {
			builder.Append(index.ToString() + ":" + vec.ToString());
			index++;
		}
		return builder.ToString();
	}

	static List<DoGASuf> importSuf(string nameFileSuf)
	{
		var context = TotekanParse.parseSuf(nameFileSuf);
		var visitor = new DoGASufVisitor();
		visitor.Visit(context as IParseTree);
		return visitor.listSufs;
	}

	static void buildSufMesh(List<DoGASuf> listSufs, ref TotekanMeshInfo infoMesh)
	{
		foreach (var suf in listSufs) {
			foreach (var mapAtrToPrim in suf.mapAtrToPrims) {
				var nameAtr = mapAtrToPrim.Key;
				var tableListPrims = mapAtrToPrim.Value;
				var listPrimsUV = tableListPrims[true];
				if (listPrimsUV.Count > 0) {
					infoMesh.namesAtr.Add(new Pair<string, bool>(nameAtr, true));
					infoMesh.indices.Add(new List<int>());
					foreach (var prim in listPrimsUV) {
						buildTablesMesh(prim, true, ref infoMesh);
					}
				}
				var listPrimsNoUV = tableListPrims[false];
				if (listPrimsNoUV.Count > 0) {
					infoMesh.namesAtr.Add(new Pair<string, bool>(nameAtr, false));
					infoMesh.indices.Add(new List<int>());
					foreach (var prim in listPrimsNoUV) {
						buildTablesMesh(prim, false, ref infoMesh);
					}
				}
			}
		}
	}

	static void buildTablesMesh(PrimIF prim, bool hasUV, ref TotekanMeshInfo infoMesh)
	{
		var listIndicesTriangle = prim.getListIndicesTriangle();
		var positions = getListVector3FromListVec3(prim.getPositions());
		List<Vector3> normals = null;
		if (prim is PrimNormalIF) {
			var primNormalIF = prim as PrimNormalIF;
			normals = normalizeListVectors(getListVector3FromListVec3(primNormalIF.getNormals()));
		} else {
			Vector3 normal = calcNormal(prim.getPositions()).normalized;
			normals = new List<Vector3>();
			for (int index = 0; index < positions.Count; index++) {
				normals.Add(normal);
			}
			for (var indexNormal = 0; indexNormal < normals.Count; indexNormal++) {
				normals[indexNormal] = -normals[indexNormal];
			}
		}

		List<Vector2> uvs = null;
		if (prim is PrimUVIF) {
			var primUVIF = prim as PrimUVIF;
			uvs = getListVector2FromListVec2(primUVIF.getUVs());
		} else {
			uvs = new List<Vector2>();
			for (int index = 0; index < positions.Count; index++) {
				uvs.Add(Vector2.zero);
			}
		}

		int indexStartInMesh = infoMesh.positions.Count;
		for (int index = 0; index < positions.Count; index++) {
			infoMesh.positions.Add(positions[index]);
			infoMesh.normals.Add(normals[index]);
			infoMesh.uvs.Add(uvs[index]);
		}

		foreach (var indicesTriangle in listIndicesTriangle) {
			var indicesTriangleInMesh = infoMesh.indices[infoMesh.indices.Count - 1];// new List<int>();
			foreach (var index in indicesTriangle) {
				indicesTriangleInMesh.Add(index + indexStartInMesh);
			}
		}
	}

	static List<Vector3> transformListPositions(List<Vector3> vecs, Matrix4x4 mat)
	{
		var ret = new List<Vector3>();
		foreach (var vec in vecs) {
			ret.Add(mat.MultiplyPoint(vec));
		}
		return ret;
	}

	static List<Vector3> transformListVectors(List<Vector3> vecs, Matrix4x4 mat)
	{
		var ret = new List<Vector3>();
		foreach (var vec in vecs) {
			ret.Add(mat.MultiplyVector(vec));
		}
		return ret;
	}

	static List<Vector3> normalizeListVectors(List<Vector3> vecs)
	{
		var ret = new List<Vector3>();
		foreach (var vec in vecs) {
			ret.Add(vec.normalized);
		}
		return ret;
	}

	static Vector3 calcNormal(List<Vec3> positions)
	{
		var num = positions.Count;
		if (num < 3) return Vector3.zero;
		if (num == 3) {
			var vec01 = getVector3FromVec3(positions[1]) - getVector3FromVec3(positions[0]);
			var vec02 = getVector3FromVec3(positions[2]) - getVector3FromVec3(positions[0]);
			return Vector3.Cross(vec02, vec01);
		} else {
			var normal = Vector3.zero;
			for (int index = 0; index < num; index++) {
				var index1 = (index + 1) % num;
				var index2 = (index + 2) % num;
				var vec10 = getVector3FromVec3(positions[index]) - getVector3FromVec3(positions[index1]);
				var vec21 = getVector3FromVec3(positions[index2]) - getVector3FromVec3(positions[index1]);
				normal += Vector3.Cross(vec10, vec21);
			}
			return normal / num;
		}
	}

	static List<Vector3> getListVector3FromListVec3(List<Vec3> vecs)
	{
		var ret = new List<Vector3>();
		foreach (var vec in vecs) {
			ret.Add(getVector3FromVec3(vec));
		}
		return ret;
	}

	static List<Vector2> getListVector2FromListVec2(List<Vec2> vecs)
	{
		var ret = new List<Vector2>();
		foreach (var vec in vecs) {
			ret.Add(getVector2FromVec2(vec));
		}
		return ret;
	}

	static Vector3 getVector3FromVec3(Vec3 vec)
	{
		return new Vector3(vec.x, vec.y, vec.z);
	}

	static Vector2 getVector2FromVec2(Vec2 vec)
	{
		return new Vector2(vec.x / 255.0f, vec.y / 255.0f);
	}

	static void getMatrixTransform(List<TransformElement> transforms, out Vector3 positionRet, out Quaternion quaternionRet, out Vector3 scaleRet)
	{
		positionRet = Vector3.zero;
		quaternionRet = Quaternion.identity;
		scaleRet = new Vector3(1.0f, 1.0f, 1.0f);
		foreach (var transform in transforms) {
			var typeTransform = transform.typeTransform;
			switch (typeTransform) {
				case "mov": {
						var position = getVector3(transform.values);
						positionRet += position;
						break;
					}
				case "rotx": {
						var angle = transform.values[0];
						quaternionRet *= Quaternion.AngleAxis(angle, new Vector3(1.0f, 0.0f, 0.0f));
						break;
					}
				case "roty": {
						var angle = transform.values[0];
						quaternionRet *= Quaternion.AngleAxis(angle, new Vector3(0.0f, 1.0f, 0.0f));
						break;
					}
				case "rotz": {
						var angle = transform.values[0];
						quaternionRet *= Quaternion.AngleAxis(angle, new Vector3(0.0f, 0.0f, 1.0f));
						break;
					}
				case "scal": {
						var scale = getVector3(transform.values);
						scaleRet.Set(scaleRet.x * scale.x, scaleRet.y * scale.y, scaleRet.z * scale.z);
						break;
					}
			}
		}
	}

	static Vector3 getVector3(float[] values)
	{
		System.Diagnostics.Debug.Assert(values.Length == 3);
		return new Vector3(values[0], values[1], values[2]);
	}

}

