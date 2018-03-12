using UnityEngine;
using System.Collections;
using RSG;
using Jobs;
using Google.Protobuf;
using System;

public class ScreenShotMain : BaseHandler
{
    bool captureNextUpdate = false;
    Promise<ProtoMessage> promise;
    ProtoScreenShot request;

	// Use this for initialization
	void Start()
	{

	}

	// Update is called once per frame
	void LateUpdate()
	{
        if(captureNextUpdate)
        {
            screenShotImmediate();
			promise = null;
            request = null;
            captureNextUpdate = false;
		}
    }

    void screenShotImmediate()
    {
        Debug.Log("Screenshotting on dispatch thread");
        try
        {
            var ss = TextureScale.CreateScreenshotPNG(Camera.main, request.Width, request.Height);
            ProtoScreenShot response = new ProtoScreenShot()
            {
                Width = request.Width,
                Height = request.Height,
                Channels = 3,
                Data = ByteString.CopyFrom(ss)
            };
            promise.Resolve(new ProtoMessage(response));
        }
        catch (Exception e)
        {
            Debug.Log($"failed screenshot: {e}");
            promise.Reject(e);
        }
    }

    public Promise<ProtoMessage> ScreenShotNextUpdate(ProtoScreenShot request)
    {
        var promise = new Promise<ProtoMessage>();
        Debug.Log("Dispatching screenshot");

        captureNextUpdate = true;
        this.promise = promise;
        this.request = request;

        return promise; 
    }



}
