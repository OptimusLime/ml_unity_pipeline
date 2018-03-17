using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RSG;
using Jobs;
using System;
using System.Linq;

public class VineView : BaseHandler {

    public GameObject FlowerPrefab;
    public GameObject LeafPrefab;
    public GameObject BranchPrefab;

    Promise<ProtoMessage> promised = null;
    PCGVineView vineScene = null;
    List<GameObject> inScene = new List<GameObject>();
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {


        //Vector3 screenPos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10); 
        //Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        //Debug.Log($"sp : {screenPos}");
        //Debug.Log($"wp : {worldPos}");

        if(this.vineScene != null){
         
            try
            {
                // turn vine into our scene of interest
                renderScene(vineScene);

                ProtoHello hi = new ProtoHello();
                var screenSize = this.vineScene.ImgSize;
                
                hi.ProtoMessage = $"delayed screen stuff {screenSize[0]}, {screenSize[1]}";

                this.promised.Resolve(new ProtoMessage(hi));
            }
            catch (Exception ex)
            {
                this.promised.Reject(ex);
            }

            this.promised = null;
            this.vineScene = null;
        }
	}


    void renderScene(PCGVineView scene)
    {
        var screenSize = this.vineScene.ImgSize;
        Screen.SetResolution(screenSize[0], screenSize[1], false);
        Debug.Log($"Changed resolution : { Screen.width}, { Screen.height}");


        clearScene();
        Debug.Log($"Rendering : {scene.Vines.Count} objects");


        var bottomLeft = Camera.main.ScreenToWorldPoint(new Vector3(0, 0, 10));//Camera.main.nearClipPlane));
        var topRight = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 10));//Camera.main.nearClipPlane));

        // here are the actual screen widths in real points 
        var swidth = Mathf.Abs(topRight.x - bottomLeft.x);
        var sheight = Mathf.Abs(topRight.y - bottomLeft.y);

        var widthHeight = new Vector2(swidth, sheight);
        var screenPosition = bottomLeft;

        //float vwidth = scene.Viewport.Xmax - scene.Viewport.Xmin;
        //float vheight = scene.Viewport.Ymax - scene.Viewport.Ymin;
        //var scale = new Vector2(swidth / vwidth, sheight / vheight);

        //// therefore we have a ratio between the size of the screen
        //// and the viewport
        //// I also think 0,0 is the center of the screen 
        //var startPoint = new Vector3(bottomLeft.x + swidth / 2 + scene.Viewport.Xmin*scale.x,
                                     //bottomLeft.y + sheight / 2 + scene.Viewport.Ymin*scale.y,
                                     //bottomLeft.z);


        //Debug.Log($"start point {startPoint}");

        // sort by N (depth), and render
        foreach (var pcgObj in scene.Vines.OrderBy(x => x.N))
        {
            Debug.Log($"Render individual: {pcgObj.Type}");
            screenPosition = renderPCGObject(pcgObj, screenPosition, widthHeight);
        }
    }

    GameObject createGameObject(GameObject prefab)
    {
        GameObject go = Instantiate(prefab);
        go.SetActive(true);
        go.transform.parent = this.transform;
        inScene.Add(go);
        return go;
    }

    void clearScene()
    {
        foreach (var go in inScene)
            Destroy(go);
        inScene.Clear();
    }

    Vector3 viewportToWorld(Vector3 pos, 
                            Vector3 screenPosition, 
                            Vector2 widthHeight)
    {
        if (this.vineScene == null)
            return pos;
        
        var viewport = this.vineScene.Viewport;
        var xrange = viewport.Xmax - viewport.Xmin;
        var yrange = viewport.Ymax - viewport.Ymin;

        return screenPosition + new Vector3(((pos.x - viewport.Xmin) / xrange)*widthHeight.x,
                                            (1 - (pos.y - viewport.Ymin) / yrange)*widthHeight.y,
                                            pos.z);

    }

    Vector3 renderLeaf(PCGLeaf leaf, Vector3 screenPosition, Vector2 widthHeight)
    {
        var go = createGameObject(this.LeafPrefab);
        //SpriteRenderer sprite = go.GetComponent<SpriteRenderer>();

        // need to position relative to the screen 
        go.transform.position = viewportToWorld(new Vector3(leaf.Center.X, leaf.Center.Y, 0),
                                                screenPosition,
                                                widthHeight);
        
        // rotate the angles (Z controls rotation)
        go.transform.eulerAngles = new Vector3(0, 0, 180*leaf.Angle/Mathf.PI);
           
        //scale according to length and width
        go.transform.localScale = new Vector3(leaf.Length, leaf.Width, 1);

        // no changes to start point 
        return screenPosition;
    }

    Vector3 renderBranch(PCGBranch branch, Vector3 screenPosition, Vector2 widthHeight)
    {
        var go = createGameObject(this.BranchPrefab);

        LineRenderer lineObject = go.GetComponent<LineRenderer>();


        var startPosition = viewportToWorld(new Vector3(branch.Start.X, branch.Start.Y, 0),
                                            screenPosition,
                                            widthHeight);
        startPosition.z = 0;

        var endPosition = viewportToWorld(new Vector3(branch.End.X, branch.End.Y, 0),
                                          screenPosition,
                                          widthHeight);
        endPosition.z = 0;
        
        lineObject.SetPosition(0, startPosition);
        lineObject.SetPosition(1, endPosition);
        lineObject.useWorldSpace = true;
        lineObject.startWidth = branch.Width;
        lineObject.endWidth = branch.Width;

        //scale according to global scale value
        //go.transform.localScale = new Vector3(scale.x, scale.y, 1);

        //go.transform.eulerAngles = new Vector3(0, 0, 180 * branch.Angle / Mathf.PI);

        // need to position relative to the screen 
        //go.transform.position = go.transform.position + startPoint;
            //+ new Vector3(startPoint.x, startPoint.y, 0);

        return screenPosition;
    }

    Vector3 renderFlower(PCGFlower flower, Vector3 screenPosition, Vector2 widthHeight)
    {
        var go = createGameObject(this.FlowerPrefab);
 
        // need to position relative to the screen 
        go.transform.position = viewportToWorld(new Vector3(flower.Center.X, flower.Center.Y, 0),
                                                screenPosition,
                                                widthHeight);
            //go.transform.position 
            //+ new Vector3(flower.Center.X + startPoint.x, 
                          //flower.Center.Y + startPoint.y, startPoint.z);

        // rotate the angles (Z controls rotation)
        go.transform.eulerAngles = new Vector3(0, 0, 180*flower.Angle/Mathf.PI);
           
        //scale according to radius 2x sucka, we got diameter up in this biatch
        go.transform.localScale = new Vector3(flower.Radius*2, 
                                              flower.Radius*2, 1);

        return screenPosition;
    }

    Vector3 renderPCGObject(PCGVineItem item, Vector3 screenPosition, Vector2 widthHeight)
    {
        if (item.Type == "leaf")
            return renderLeaf(item.Leaf, screenPosition, widthHeight);
        else if (item.Type == "branch")
            return renderBranch(item.Branch, screenPosition, widthHeight);
        else if (item.Type == "flower")
            return renderFlower(item.Flower, screenPosition, widthHeight);
        else
            throw new NotImplementedException($"Unknown item type {item.Type}");
    }

    public Promise<ProtoMessage> RenderVines(PCGVineView vines)
    {
        var promise = new Promise<ProtoMessage>();
        vineScene = vines;
        this.promised = promise;

        return promise; 

    }

}
