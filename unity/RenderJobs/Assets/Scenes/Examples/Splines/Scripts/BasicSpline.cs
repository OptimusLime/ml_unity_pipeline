using UnityEngine;
using System.Collections;
using Dreamteck.Splines; //Include the Splines namespace
using RSG;
using Jobs;

//[ExecuteInEditMode]
public class BasicSpline : BaseHandler
{
    public float Width = .5f;
    public float Radius = 10;
    public int Points = 5;

    ProtoSpline splineToDraw;
    Promise<ProtoMessage> toResolve;

    SplinePoint pointToLocation(float radius, float width, Vector3 location = default(Vector3))
    {
        var rLoc = location == default(Vector3) ? radius * UnityEngine.Random.insideUnitSphere : location;

        var loc2D = new Vector3(rLoc.x, rLoc.y, 0);
        var sp = new SplinePoint(loc2D);
        sp.size = width;

        return sp;
    }
    void Update()
    {
        if(splineToDraw != null)
        {
            DrawSpline(splineToDraw);
            ProtoHello hello = new ProtoHello();
            hello.ProtoMessage = "go fuck yourself with this bullshit";
            toResolve.Resolve(new ProtoMessage(hello));
            splineToDraw = null;
            toResolve = null;
        }
    }

    public Promise<ProtoMessage> RenderVines(ProtoSpline splines)
    {
        splineToDraw = splines;
        toResolve = new Promise<ProtoMessage>();
        return toResolve;
    }

    static Vector3 protoV3ToV3(ProtoV3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    void DrawSpline(ProtoSpline spline)
    {
        Screen.SetResolution(spline.ScreenWidth, spline.ScreenHeight, false);
        var sc = this.gameObject.GetComponent<SplineComputer>();
        if (sc == null)
            sc = this.gameObject.AddComponent<SplineComputer>();

        sc.space = SplineComputer.Space.Local;
        //base.Start();

        //Create a new B-spline with precision 0.9
        //Spline spline = new Spline(Spline.Type.BSpline, 0.9);

        //Create 3 control points for the spline
        var sp = new SplinePoint[spline.ControlPoints.Count];
        for (var i = 0; i < spline.ControlPoints.Count; i++)
            sp[i] = pointToLocation(Radius, Width, protoV3ToV3(spline.ControlPoints[i]));

        //set points in computer 
        sc.SetPoints(sp, SplineComputer.Space.Local);

        ////Evaluate the spline and get an array of values
        //SplineResult[] results = new SplineResult[sc.iterations];
        //sc.Evaluate(ref results);
    }
}