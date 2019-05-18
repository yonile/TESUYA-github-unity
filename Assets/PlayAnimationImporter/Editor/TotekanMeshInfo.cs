using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TotekanReaderLib;


public class TotekanMeshInfo
{
	public List<Vector3> positions = new List<Vector3>();
	public List<Vector3> normals = new List<Vector3>();
	public List<Vector2> uvs = new List<Vector2>();
	public List<List<int>> indices = new List<List<int>>();
    public List<Pair<string, bool>> namesAtr = new List<Pair<string, bool>>();
}
