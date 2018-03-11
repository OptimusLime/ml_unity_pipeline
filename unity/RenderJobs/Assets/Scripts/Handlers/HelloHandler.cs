using UnityEngine;
using System.Collections;
using System.Reflection;
using Jobs;
using RSG;
using System;

public class HelloHandler : MonoBehaviour
{
	// Use this for initialization
	void Start()
	{
        // what a hack, I just want to register a function for this proto
        // maybe there is a better way, but this is the only that can match generic types
        var methodInfo = this.GetType().GetMethod("ReceiveHello", BindingFlags.NonPublic);

        //register our response to hello messages :)
        ProtoRouter.Instance.AddResponsePromise(typeof(ProtoHello), this, methodInfo);
	}


    //Handle incoming hellos -- and send a response when we're done
    public Promise<ProtoMessage> ReceiveHello(ProtoHello hello)
    {
        return new Promise<ProtoMessage>((resolve, reject) =>
        {
            ProtoHello response = new ProtoHello();
            response.ProtoMessage = $"howdy there from c# - you said {hello.ProtoMessage}";
            Debug.Log($"receiving a friendly hello and responding {hello.ProtoMessage}");

            resolve(new ProtoMessage(response));
        });
    }

	// Update is called once per frame
	void Update()
	{
			
	}
}
