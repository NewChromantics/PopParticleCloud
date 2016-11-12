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
	public bool		CenterMesh = true;
	public bool		MoveSelf = true;

	[Range(0,1)]
	public float	Density = 1.0f;
	public bool		ModifySharedMaterial = true;
	public bool		GenerateAsPoints = true;
	public string	PointGeometryShaderFeature = "POINT_TOPOLOGY";

	public bool		UseVertexTriangulation = false;
	public string	VertexTriangulationShaderFeature = "VERTEX_TRIANGULATION";

	void Update ()
	{
		if (!Dirty)
			return;

		GenerateNewMesh ();
		Dirty = false;
	}

	public void GenerateNewMesh()
	{
		if (Density == 0.0f) {
			throw new System.Exception ("Density is " + Density + " cannot create mesh");
		}
		//	create mesh
		PointMesh = new Mesh();

		Debug.Log ("parsing " + Filename);
		var Lines = System.IO.File.ReadAllLines (Filename);
		var Positions = new List<Vector3> ();
		var Normals = new List<Vector3> ();
		var Colours = new List<Color> ();
		bool BoundsInitialised = false;
		Bounds MinMax = new Bounds ();

		//	generate indicies
		int VertexesPerTriangle = ( GenerateAsPoints ? 1 : 3 );


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

					if ( GenerateAsPoints )
					{
						Positions.Add( Pos3 );
						Colours.Add( Colour3 );
						Normals.Add( new Vector3(1,0,0) );
					}
					else
					{
						Positions.Add( Pos3 );
						Colours.Add( Colour3 );
						Normals.Add( new Vector3(1,0,0) );

						Positions.Add( Pos3 );
						Colours.Add( Colour3 );
						Normals.Add( new Vector3(0,1,0) );

						Positions.Add( Pos3 );
						Colours.Add( Colour3 );
						Normals.Add( new Vector3(0,0,1) );
					}
				} 
				catch (System.Exception e) 
				{
					Debug.LogWarning ("Exception with line: " + Line + ": " + e.Message);
				}
			}
					
		}

		//	never added any verts
		if (!BoundsInitialised) {
			throw new System.Exception ("Failed to parse any points");
		}


		//	center verts
		if (CenterMesh) {
			var BoundsCenter = MinMax.center;
			for (int i = 0;	i < Positions.Count;	i++) {
				Positions [i] -= BoundsCenter;
			}
			MinMax.min -= BoundsCenter;
			MinMax.max -= BoundsCenter;

			if (MoveSelf)
				this.transform.localPosition = BoundsCenter;
		}


		//	crop for density
		if (Density < 1) 
		{
			var OldCount = Positions.Count;
			var Step = 1.0f / (1.0f-Density);
			if (Step <= 1.0f)
				throw new System.Exception ("Step is too low " + Step);

			if (GenerateAsPoints) {
				for (float i = Positions.Count - 1;	i >= 0;	i -= Step) {
					Positions.RemoveAt ((int)i);
					Normals.RemoveAt ((int)i);
					Colours.RemoveAt ((int)i);
				}
			} else {
				int TriangleCount = Positions.Count / 3;
				for (float i = TriangleCount - 1;	i >= 0;	i -= Step) {

					var ti = (int)i * 3;
					Positions.RemoveRange(ti, 3 );
					Normals.RemoveRange (ti, 3 );
					Colours.RemoveRange (ti, 3 );
				}
			}

			var NewDensity = Positions.Count / (float)OldCount;
			Debug.Log ("Reduced points from " + OldCount + " to " + Positions.Count + "(" + (Density*100) + "% requested, produced " + (NewDensity*100) + "%)");
		}

		//	cap to the unity limit
		var VertexLimit = 65535 - (GenerateAsPoints ? 1 : 3);
		if (Positions.Count >= VertexLimit) {
			var Dropped = Positions.Count - VertexLimit;
			Debug.LogWarning ("capped point cloud to vertex limit of " + VertexLimit + " from " + Positions.Count + ". " + Dropped + " dropped");
			Positions.RemoveRange (VertexLimit, Dropped);
			Normals.RemoveRange (VertexLimit, Dropped);
			Colours.RemoveRange (VertexLimit, Dropped);
		}


		//  generate indicies 
		var Indexes = new int[Positions.Count]; 
		for (int i = 0;  i < Indexes.Length;  i++) 
			Indexes [i] = i; 

		PointMesh.SetVertices( Positions );
		PointMesh.SetNormals (Normals);
		PointMesh.SetColors (Colours);
		PointMesh.SetIndices (Indexes, GenerateAsPoints ? MeshTopology.Points : MeshTopology.Triangles, 0);
		PointMesh.bounds = MinMax;


		var mf = this.GetComponent<MeshFilter> ();
		mf.mesh = PointMesh;

		var mr = this.GetComponent<MeshRenderer> ();
		var mat = ModifySharedMaterial ? mr.sharedMaterial : mr.material;

		if (GenerateAsPoints)
			mat.EnableKeyword (PointGeometryShaderFeature);
		else
			mat.DisableKeyword (PointGeometryShaderFeature);

		if ( UseVertexTriangulation )
			mat.EnableKeyword (VertexTriangulationShaderFeature);
		else
			mat.DisableKeyword (VertexTriangulationShaderFeature);


		var bc = this.GetComponent<BoxCollider> ();
		if (bc != null) {
			bc.enabled = false;
			bc.enabled = true;
		}

	}
}
