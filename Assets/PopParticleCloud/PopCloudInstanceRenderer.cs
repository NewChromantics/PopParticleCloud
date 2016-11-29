using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PopCloudInstanceRenderer : MonoBehaviour {

	[InspectorButton("Generate")]
	public bool	Dirty = true;

	[InspectorButton("GenerateChildren")]
	public bool	DirtyChildren = true;
	public bool	AlwaysDirtyChildren = true;

	[InspectorButton("EnableChildren")]
	public bool	EnableChildrenx = true;


	public List<Matrix4x4>	InstanceMatrixes;

	void Update () {

		if (DirtyChildren)
		{
			GenerateChildren();

			if ( !AlwaysDirtyChildren )
				DirtyChildren = false;
		}


		if (Dirty)
		{
			Generate();
			Dirty = false;
		}

		RenderInstances();
	}

	public void RenderInstances()
	{
		var mf = GetComponent<MeshFilter>();
		var mr = GetComponent<MeshRenderer>();

		var CloudMaterial = GetComponent<MeshRenderer>();

		//Debug.Log("Rendering " + InstanceMatrixes.Count + " isntances");

        Graphics.DrawMeshInstanced( mf.sharedMesh, 0, mr.sharedMaterial, InstanceMatrixes );

		//	now we're drawing the isntances, don't draw self
		mr.enabled = false;
    }

	public void Generate()
	{
		var PointMesh = new Mesh();

		PointMesh.name = this.name;

		var Positions = new List<Vector3> ();
		var Normals = new List<Vector3> ();
		var Colours = new List<Color> ();
		bool BoundsInitialised = false;
		Bounds MinMax = new Bounds ();

		var MeshWidth = 147;
		var MeshHeight = 147;

		//	decode points
		{
			var Pos3 = new Vector3 ();
			var Colour3 = new Color();
			var Space = new char[]{ ' ' };
			/*
			Vector3 Offa = new Vector3(0.01f,0,0);
			Vector3 Offb = new Vector3(0,0.01f,0);
			Vector3 Offc = new Vector3(0,0,0.01f);
			*/
			Vector3 Offa = Vector3.zero;
			Vector3 Offb = Vector3.zero;
			Vector3 Offc = Vector3.zero;

			for ( var x=0;	x<MeshWidth;	x++ )
			{
				for ( var y=0;	y<MeshHeight;	y++ )
				{
					Pos3.x = x / (float)MeshWidth;
					Pos3.z = y / (float)MeshHeight;
					Pos3.y = Mathf.PerlinNoise( Pos3.x, Pos3.z );

					Colour3.r = Pos3.x;
					Colour3.g = Pos3.z;
					Colour3.b = 0;

					if ( !BoundsInitialised )
					{
						MinMax = new Bounds (Pos3, Vector3.zero );
						BoundsInitialised = true;
					}
					MinMax.Encapsulate( Pos3 );


					Positions.Add( Pos3+Offa );
					Colours.Add( Colour3 );
					Normals.Add( new Vector3(1,0,0) );

					Positions.Add( Pos3+Offb );
					Colours.Add( Colour3 );
					Normals.Add( new Vector3(0,1,0) );

					Positions.Add( Pos3+Offc );
					Colours.Add( Colour3 );
					Normals.Add( new Vector3(0,0,1) );
				} 
			}
					
		}

		//  generate indicies 
		var Indexes = new int[Positions.Count]; 
		for (int i = 0;  i < Indexes.Length;  i++) 
			Indexes [i] = i; 

		PointMesh.SetVertices( Positions );
		PointMesh.SetNormals (Normals);
		PointMesh.SetColors (Colours);
		PointMesh.SetIndices (Indexes, MeshTopology.Triangles, 0);
		PointMesh.bounds = MinMax;

		var mf = GetComponent<MeshFilter>();
		mf.sharedMesh = PointMesh;
	}

	public void GenerateChildren()
	{
		InstanceMatrixes = new List<Matrix4x4>();
		var Children = GetComponentsInChildren<MeshRenderer>(true);
		foreach ( var Child in Children )
		{
			if ( Child.transform == this.transform )
				continue;

			InstanceMatrixes.Add( Child.transform.localToWorldMatrix );

			Child.gameObject.SetActive(false);
		}
	}

	public void EnableChildren()
	{
		var Children = GetComponentsInChildren<MeshRenderer>(true);
		foreach ( var Child in Children )
		{
			if ( Child.transform == this.transform )
				continue;

			Child.gameObject.SetActive(true);
		}
	}
	

}
