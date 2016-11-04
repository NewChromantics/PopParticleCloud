using UnityEngine;
using System.Collections;
using System.Collections.Generic;


[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PointcloudToMesh : MonoBehaviour {

	public bool		Dirty = true;
	public string	Filename;
	public Mesh		PointMesh;
	public bool		Center = true;
	public bool ModifySharedMaterial = true;
	public string	EnableShaderFeature = "POINT_GEOMETRY";

	void Update () 
	{
		if (!Dirty )
			return;

		Dirty = false;

		//	create mesh
		PointMesh = new Mesh();

		Debug.Log ("parsing " + Filename);
		var Lines = System.IO.File.ReadAllLines (Filename);
		var Positions = new List<Vector3> ();
		var Colours = new List<Color> ();
		bool BoundsInitialised = false;
		Bounds MinMax = new Bounds ();

		//	decode points
		{
			var Pos3 = new Vector3 ();
			var Colour3 = new Color();
			var Space = new char[]{ ' ' };
	
			foreach (string Line in Lines) {
				try {
					var Floats = Line.Split (Space);
					Pos3.x = FastParse.Float (Floats [0]);
					Pos3.y = FastParse.Float (Floats [1]);
					Pos3.z = FastParse.Float (Floats [2]);
					Colour3.r = FastParse.Float (Floats [3]) / 256.0f;
					Colour3.g = FastParse.Float (Floats [4]) / 256.0f;
					Colour3.b = FastParse.Float (Floats [5]) / 256.0f;

					if ( !BoundsInitialised )
					{
						MinMax = new Bounds (Pos3, Vector3.zero );
						BoundsInitialised = true;
					}

					MinMax.Encapsulate( Pos3 );
					Positions.Add( Pos3 );
					Colours.Add( Colour3 );
				} 
				catch (System.Exception e) 
				{
					Debug.LogWarning ("Exception with line: " + Line + ": " + e.Message);
				}
			}
					
		}

		//	center verts
		if (Center) {
			var BoundsCenter = MinMax.center;
			for (int i = 0;	i < Positions.Count;	i++) {
				Positions [i] -= BoundsCenter;
			}
			MinMax.min -= BoundsCenter;
			MinMax.max -= BoundsCenter;
		}

		//	generate indicies
		var Indexes = new int[Positions.Count];
		for (int i = 0;	i < Indexes.Length;	i++)
			Indexes [i] = i;

		PointMesh.SetVertices( Positions );
		PointMesh.SetColors (Colours);
		PointMesh.SetIndices (Indexes, MeshTopology.Points, 0);
		PointMesh.bounds = MinMax;


		var mf = this.GetComponent<MeshFilter> ();
		mf.mesh = PointMesh;

		var mr = this.GetComponent<MeshRenderer> ();
		if ( ModifySharedMaterial )
			mr.sharedMaterial.EnableKeyword (EnableShaderFeature);
		else
			mr.material.EnableKeyword (EnableShaderFeature);

		var bc = this.GetComponent<BoxCollider> ();
		if (bc != null) {
			bc.enabled = false;
			bc.enabled = true;
		}

		Dirty = false;
	}
}
