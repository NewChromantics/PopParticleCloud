using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class PointcloudLoader : MonoBehaviour {

	public bool				Dirty = true;
	public PointcloudToMesh	Template;
	public string			Path;
	public List<string>		LoadQueue = new List<string>();

	public List<GameObject>	GeneratedClouds = new List<GameObject>();


	void LoadPointCloud(string Filename)
	{
		var NewCloud = GameObject.Instantiate (Template);
		NewCloud.transform.SetParent (this.transform);
		GeneratedClouds.Add (NewCloud.gameObject);

		NewCloud.Filename = Filename;
		NewCloud.GenerateNewMesh();
		NewCloud.gameObject.SetActive (true);
	}

	void ClearClouds()
	{
		foreach (var go in GeneratedClouds) {
			Destroy (go);
		}
		GeneratedClouds.Clear ();
	}


	void Update () {
	
		if (Dirty) {

			ClearClouds ();
			LoadQueue.Clear ();

			try
			{
				var Dir = new System.IO.DirectoryInfo( Path );
				var Files = Dir.GetFiles();
				foreach ( var File in Files )
				{
					LoadQueue.Add( File.FullName );
				}
			}
			catch( System.Exception e ) {
				Debug.LogWarning ("failed to iterate path: " + Path + ": " + e.Message);
			}
			
			Dirty = false;
		}

		if (Template == null) {
			Debug.LogWarning ("failed to load, no template");
			return;
		}


		//	load next
		if (LoadQueue.Count > 0) {
			var Filename = LoadQueue [0];
			LoadQueue.RemoveAt (0);
			LoadPointCloud (Filename);
		}

	}
}
