using UnityEngine;
using System.Collections;
using Dreamteck.Splines; //Include the Splines namespace
using RSG;
using Jobs;
using System;
using System.Collections.Generic;
using BeautifyEffect;

//[ExecuteInEditMode]
public class BasicTraffic : BaseHandler
{
    ProtoTrafficScene sceneToDraw;
    Promise<ProtoMessage> toResolve;
    public Beautify TrafficCam;


    public GameObject TrafficPrefab;
    public GameObject ParentHolder;
    List<GameObject> gameObjects = new List<GameObject>();

    float timeTillRender = 2.0f;
    bool waitingOnWeather = false;

    void Update()
    {
        if(sceneToDraw != null && !waitingOnWeather)
        {
            DrawTrafficScene(sceneToDraw);
            timeTillRender = sceneToDraw.WaitTime;
            waitingOnWeather = true;
            // resolveScenePromise();
        }

        if(waitingOnWeather)
        {
            timeTillRender -= Time.deltaTime;

            if(timeTillRender < 0)
            {
                resolveScenePromise();
                waitingOnWeather = false;
            }
        }
    }

    void resolveScenePromise()
    {
        if(sceneToDraw != null)
        {
            // ProtoHello hello = new ProtoHello();
            // hello.ProtoMessage = "finished rendering traffic scene";
            toResolve.Resolve(new ProtoMessage(sceneToDraw));
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

    void setWeather(int ix)
    {
        EnviroSky.instance.SetWeatherOverwrite(ix);
        // EnviroSky.instance.UpdateWeather();

    }

    void DrawTrafficScene(ProtoTrafficScene scene)
    {

        try
        {
            clearScene();
            Debug.Log(scene);

            EnviroSky.instance.GameTime.ProgressTime = EnviroTime.TimeProgressMode.None;

            Screen.SetResolution(scene.ScreenWidth, scene.ScreenHeight, false);

            // process our scene object
            // TODO: Add time of day support
            var time = scene.Environment.HourOfDay; // hour_of_day
            EnviroSky.instance.SetInternalTimeOfDay(time);

            // EnviroSky.instance.UpdateTime();
            // EnviroSky.instance.UpdateSunAndMoonPosition();
            // EnviroSky.instance.CalculateDirectLight();
            // EnviroSky.instance.UpdateAmbientLight();
            // EnviroSky.instance.UpdateReflections();
            // EnviroSky.instance.RenderMoon();

            if(time > 7 && time < 18)
            {
                TrafficCam.bloom = false;
            }
            else
            {
                TrafficCam.bloom = true;
            }

            var weather = (int) scene.Environment.Weather;
            setWeather(weather);


            foreach(var trafficLight in scene.TrafficLights)
            {
                // instantiate the traffic light
                var trafficObject = GameObject.Instantiate(TrafficPrefab);
                // trafficObject.transform.parent = ParentHolder.transform;

                if(trafficLight.Location != null)
                    trafficObject.transform.position = protoV3ToV3(trafficLight.Location);
                if(trafficLight.Orientation != null)
                    trafficObject.transform.rotation = Quaternion.Euler(protoV3ToV3(trafficLight.Orientation));

                // trafficObject.transform.position = Vector3.zero;
                // trafficObject.transform.rotation = Quaternion.identity;


                var light = trafficObject.GetComponent<QT_TrafficLight>();

                setLight(trafficLight.LightStatus, light);

                var meshBounds = trafficObject.GetComponent<ShowMeshBounds>();
                if(meshBounds != null)
                {
                    var meshRect = meshBounds.getMeshRectUI();
                    var renderRect = trafficLight.RenderLoc;
                    if(renderRect == null)
                    {
                        renderRect = new ProtoRenderRect();
                        trafficLight.RenderLoc = renderRect;
                    }
                    // Debug.Log(string.Format("{0} and {1}", renderRect, meshRect));
                    renderRect.XMin = meshRect.xMin;
                    renderRect.XMax = meshRect.xMax;
                    renderRect.YMin = meshRect.yMin;
                    renderRect.YMax = meshRect.yMax;
                    // Debug.Log(string.Format("rect {0}", meshRect));
                }
                else
                {
                    //
                    Debug.Log("Missing the ShowMeshComponent");
                }
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