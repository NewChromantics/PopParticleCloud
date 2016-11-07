using UnityEngine;
using System.Collections;
using System.Collections.Generic;



public class PointcloudLoader : MonoBehaviour {

	public bool				Dirty = true;
	public PointcloudToMesh	Template;
	public List<string>		Paths;
	public List<string>		ExcludeMatching = new List<string> ( new string[]{".meta"});
	public List<string>		OnlyMatching = new List<string> ();
	public List<string>		LoadQueue = new List<string>();

	public List<GameObject>	GeneratedClouds = new List<GameObject>();


	void LoadPointCloud(string Filename)
	{
		var NewCloud = GameObject.Instantiate (Template);

		//	if this throws we don't store the object
		NewCloud.Filename = Filename;
		NewCloud.GenerateNewMesh();

		NewCloud.transform.SetParent (this.transform,false);
		GeneratedClouds.Add (NewCloud.gameObject);
		NewCloud.gameObject.SetActive (true);
	}

	void ClearClouds()
	{
		foreach (var go in GeneratedClouds) {
			Destroy (go);
		}
		GeneratedClouds.Clear ();
	}

	void EnumDirectory(string Path)
	{
		var Dir = new System.IO.DirectoryInfo (Path);
		var Files = Dir.GetFiles ();

		foreach (var File in Files) 
		{
			var ShortName = File.Name;
		
			bool Excluded = false;
			foreach ( var ExcludeMatch in ExcludeMatching )
			{
				if (ShortName.Contains (ExcludeMatch))
					Excluded = true;
			}
			if (Excluded)
				continue;

			bool Matched = (OnlyMatching.Count==0);
			foreach ( var OnlyMatch in OnlyMatching )
			{
				Matched |= ShortName.Contains (OnlyMatch);
			}
				

			LoadQueue.Add (File.FullName);
		}
	}

	void Start()
	{
		Dirty = true;
	}

	void Update () {
	
		if (Dirty) {

			ClearClouds ();
			LoadQueue.Clear ();

			foreach (var _Path in Paths) {

				var Path = PopUrl.ReolveUrl (_Path);

				if (Path.StartsWith (PopUrl.FileProtocol))
					Path = Path.Substring (PopUrl.FileProtocol.Length);

				try {
						EnumDirectory( Path );
				} catch (System.Exception e) {
					Debug.LogWarning ("failed to iterate path: " + Path + ": " + e.Message);
				}
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
