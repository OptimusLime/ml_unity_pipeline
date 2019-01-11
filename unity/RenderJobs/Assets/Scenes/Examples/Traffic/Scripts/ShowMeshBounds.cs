using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//  [ExecuteInEditMode()]

 public class ShowMeshBounds : MonoBehaviour {
     public Color color = Color.green;
	// public Camera trafficCam;
     private Vector3 v3FrontTopLeft;
     private Vector3 v3FrontTopRight;
     private Vector3 v3FrontBottomLeft;
     private Vector3 v3FrontBottomRight;
     private Vector3 v3BackTopLeft;
     private Vector3 v3BackTopRight;
     private Vector3 v3BackBottomLeft;
     private Vector3 v3BackBottomRight;
 private Vector3[] pts = new Vector3[8];
 public float margin = 0;
 private Rect _rect;



//  void Update() {
//      CalcPositons();
//      DrawBox();
//  }

 void CalcPositons(){
     Bounds bounds = GetComponent<MeshFilter>().sharedMesh.bounds;

     //Bounds bounds;
     //BoxCollider bc = GetComponent<BoxCollider>();
     //if (bc != null)
     //    bounds = bc.bounds;
     //else
         //return;

     Vector3 v3Center = bounds.center;
     Vector3 v3Extents = bounds.extents;

     v3FrontTopLeft     = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top left corner
     v3FrontTopRight    = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z);  // Front top right corner
     v3FrontBottomLeft  = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom left corner
     v3FrontBottomRight = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z);  // Front bottom right corner
     v3BackTopLeft      = new Vector3(v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top left corner
     v3BackTopRight        = new Vector3(v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z);  // Back top right corner
     v3BackBottomLeft   = new Vector3(v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom left corner
     v3BackBottomRight  = new Vector3(v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z);  // Back bottom right corner

	 v3FrontTopLeft     = transform.TransformPoint(v3FrontTopLeft);
     v3FrontTopRight    = transform.TransformPoint(v3FrontTopRight);
     v3FrontBottomLeft  = transform.TransformPoint(v3FrontBottomLeft);
     v3FrontBottomRight = transform.TransformPoint(v3FrontBottomRight);
     v3BackTopLeft      = transform.TransformPoint(v3BackTopLeft);
     v3BackTopRight     = transform.TransformPoint(v3BackTopRight);
     v3BackBottomLeft   = transform.TransformPoint(v3BackBottomLeft);
     v3BackBottomRight  = transform.TransformPoint(v3BackBottomRight);
 }

// float getMinX(List<Vector3> vals)
// {
// 	float x = 100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.x < x)
// 			{
// 				x = v3.x;
// 			}
// 	}
// 	return x;
// }


// float getMinY(List<Vector3> vals)
// {
// 	float y = 100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.y < y)
// 			{
// 				y = v3.y;
// 			}
// 	}
// 	return y;
// }



// float getMaxY(List<Vector3> vals)
// {
// 	float y = -100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.y > y)
// 			{
// 				y = v3.y;
// 			}
// 	}
// 	return y;
// }

// float getMaxX(List<Vector3> vals)
// {
// 	float x = -100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.x > x)
// 			{
// 				x = v3.x;
// 			}
// 	}
// 	return x;
// }

// float getMinZ(List<Vector3> vals)
// {
// 	float z = 100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.z < z)
// 			{
// 				z = v3.z;
// 			}
// 	}
// 	return z;
// }


// float getMaxZ(List<Vector3> vals)
// {
// 	float z = -100000;
// 	foreach(var v3 in vals)
// 	{
// 		if(v3.z > z)
// 			{
// 				z = v3.z;
// 			}
// 	}
// 	return z;
// }


 public Rect getMeshRectUI () {

	var bounds = new Bounds(Vector3.zero, Vector3.zero);
	var filters = this.GetComponentsInChildren<MeshFilter>();

	foreach(var filter in filters)
	{
		//if members are not tagged group or microlabel since these are just groups
		// if(filter.tag!="group" || filter.tag!="microlabel")
		// {
		if(filter.sharedMesh != null)
			bounds.Encapsulate(filter.sharedMesh.bounds);
		// }
	}

	// Bounds b = GetComponent<MeshFilter>().sharedMesh.bounds;
     //Bounds bounds;
     //BoxCollider bc = GetComponent<BoxCollider>();
     //if (bc != null)
     //    bounds = bc.bounds;
     //else
         //return;

     Vector3 v3Center = bounds.center;
     Vector3 v3Extents = bounds.extents;

	 Camera cam = Camera.main;

     //The object is behind us
    //  if (cam.WorldToScreenPoint (bounds.center).z < 0) return ;

     //All 8 vertices of the bounds
     pts[0] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z)));
     pts[1] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x + v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z)));
     pts[2] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z)));
     pts[3] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x + v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z)));
     pts[4] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z + v3Extents.z)));
     pts[5] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x - v3Extents.x, v3Center.y + v3Extents.y, v3Center.z - v3Extents.z)));
     pts[6] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z + v3Extents.z)));
     pts[7] = cam.WorldToScreenPoint (transform.TransformPoint(new Vector3 (v3Center.x - v3Extents.x, v3Center.y - v3Extents.y, v3Center.z - v3Extents.z)));

     //Get them in GUI space
     for (int i=0;i<pts.Length;i++) pts[i].y = Screen.height-pts[i].y;

     //Calculate the min and max positions
     Vector3 min = pts[0];
     Vector3 max = pts[0];
     for (int i=1;i<pts.Length;i++) {
         min = Vector3.Min (min, pts[i]);
         max = Vector3.Max (max, pts[i]);
     }

	//  Debug.Log(string.Format("min {0}, max {1}", min, max));

     //Construct a rect of the min and max positions and apply some margin
     Rect r = Rect.MinMaxRect (min.x,min.y,max.x,max.y);
     r.xMin -= margin;
     r.xMax += margin;
     r.yMin -= margin;
     r.yMax += margin;
	 Debug.Log(string.Format(" rect {2} min {0}, max {1}", min, max, r));

     //Render the box
    //  GUI.Box (r,"");
	return r;
 }

 void OnGUI()
 {
	//  GUI.Box(getMeshRectUI(), "");
 }


//  void DrawBox() {

// 	 List<Vector3> worldV3s = new List<Vector3>();

// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3FrontTopLeft));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3FrontTopRight));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3FrontBottomRight));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3FrontBottomLeft));

// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3BackTopLeft));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3BackTopRight));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3BackBottomRight));
// 	//  worldV3s.Add(trafficCam.WorldToScreenPoint(v3BackBottomLeft));


// 	//  bottom left
// 	var xMin = getMinX(worldV3s);
// 	var xMax = getMaxX(worldV3s);

// 	var yMin = getMinY(worldV3s);
// 	var yMax = getMaxY(worldV3s);

// 	var zMin = getMinZ(worldV3s);
// 	var zMax = getMaxZ(worldV3s);


// 	Debug.Log(string.Format("xmin {0} xmax {1}", xMin, xMax));
// 	Debug.Log(string.Format("ymin {0} ymax {1}", yMin, yMax));
// 	Debug.Log(string.Format("zmin {0} zmax {1}", zMin, zMax));
// 	// now we make our debug lines
// 	Debug.DrawLine(new Vector3(xMin, yMin, zMin), new Vector3(xMin, yMax, zMin), color);
// 	Debug.DrawLine(new Vector3(xMin, yMax, zMin), new Vector3(xMax, yMax, zMin), color);
// 	Debug.DrawLine(new Vector3(xMax, yMax, zMin), new Vector3(xMax, yMin, zMin), color);
// 	Debug.DrawLine(new Vector3(xMax, yMin, zMin), new Vector3(xMin, yMin, zMin), color);


//     //  //if (Input.GetKey (KeyCode.S)) {
//      Debug.DrawLine (v3FrontTopLeft, v3FrontTopRight, color);
//      Debug.DrawLine (v3FrontTopRight, v3FrontBottomRight, color);
//      Debug.DrawLine (v3FrontBottomRight, v3FrontBottomLeft, color);
//      Debug.DrawLine (v3FrontBottomLeft, v3FrontTopLeft, color);

//     //  Debug.DrawLine (v3BackTopLeft, v3BackTopRight, color);
//     //  Debug.DrawLine (v3BackTopRight, v3BackBottomRight, color);
//     //  Debug.DrawLine (v3BackBottomRight, v3BackBottomLeft, color);
//     //  Debug.DrawLine (v3BackBottomLeft, v3BackTopLeft, color);

//     //  Debug.DrawLine (v3FrontTopLeft, v3BackTopLeft, color);
//     //  Debug.DrawLine (v3FrontTopRight, v3BackTopRight, color);
//     //  Debug.DrawLine (v3FrontBottomRight, v3BackBottomRight, color);
//     //  Debug.DrawLine (v3FrontBottomLeft, v3BackBottomLeft, color);
//      //}
//  }

 }