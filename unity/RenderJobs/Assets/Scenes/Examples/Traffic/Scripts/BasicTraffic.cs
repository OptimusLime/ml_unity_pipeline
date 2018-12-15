using UnityEngine;
using System.Collections;
using Dreamteck.Splines; //Include the Splines namespace
using RSG;
using Jobs;
using System;
using System.Collections.Generic;

//[ExecuteInEditMode]
public class BasicTraffic : BaseHandler
{
    ProtoTrafficScene sceneToDraw;
    Promise<ProtoMessage> toResolve;

    public GameObject TrafficPrefab;
    List<GameObject> gameObjects = new List<GameObject>();

    void Update()
    {
        if(sceneToDraw != null)
        {
            DrawTrafficScene(sceneToDraw);
            resolveScenePromise();
        }
    }

    void resolveScenePromise()
    {
        if(sceneToDraw != null)
        {
            ProtoHello hello = new ProtoHello();
            hello.ProtoMessage = "finished rendering traffic scene";
            toResolve.Resolve(new ProtoMessage(hello));
            sceneToDraw = null;
            toResolve = null;
        }
    }

    public Promise<ProtoMessage> RenderTrafficScene(ProtoTrafficScene sceneToRender)
    {
        sceneToDraw = sceneToRender;
        toResolve = new Promise<ProtoMessage>();
        return toResolve;
    }
    static Vector3 protoV3ToV3(ProtoVector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    void clearScene()
    {
        foreach(var go in gameObjects.ToArray())
        {
            Destroy(go);
        }
        gameObjects.Clear();
    }
    void setLight(int lightStatus, QT_TrafficLight lightScript)
    {
        lightScript.InitializeTrafficLight();
        switch(lightStatus)
        {
            case 0:
                lightScript.SetLightValue(1, 0, 0);
                break;
            case 1:
                lightScript.SetLightValue(0, 1, 0);
                break;
            case 2:
                lightScript.SetLightValue(0, 0, 1);
                break;
            default:
                throw new Exception("Unknown lightstatus " + lightStatus.ToString());

        }
    }

    void DrawTrafficScene(ProtoTrafficScene scene)
    {

        try
        {
            clearScene();
            Debug.Log(scene);

            Screen.SetResolution(scene.ScreenWidth, scene.ScreenHeight, false);

            // process our scene object
            // TODO: Add time of day support
            var time = scene.Environment.HourOfDay; // hour_of_day
            var weather = scene.Environment.Weather;

            foreach(var trafficLight in scene.TrafficLights)
            {
                // instantiate the traffic light
                var trafficObject = GameObject.Instantiate(TrafficPrefab);
                trafficObject.transform.position = protoV3ToV3(trafficLight.Location);
                trafficObject.transform.rotation = Quaternion.Euler(protoV3ToV3(trafficLight.Orientation));

                var light = trafficObject.GetComponent<QT_TrafficLight>();

                setLight(trafficLight.LightStatus, light);

                // add to list of things to clear scene
                gameObjects.Add(trafficObject);
            }
        }
        catch (System.Exception e)
        {
            // we fucked up, resolve the promise (eat the scene) and log it
            resolveScenePromise();
            Debug.LogError(e);
        }


    }
}