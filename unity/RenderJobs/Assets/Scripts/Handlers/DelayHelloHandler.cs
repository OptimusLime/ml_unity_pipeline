using UnityEngine;
using System.Collections;
using System.Reflection;
using Jobs;
using RSG;
using System;

public class DelayHelloHandler : MonoBehaviour
{

    ProtoMessage futureMessage = null;
    Promise<ProtoMessage> promised = null;
    float delay = 4.0f;

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
        var promise = new Promise<ProtoMessage>();

        this.promised = promise;
        Debug.Log($"receiving a friendly hello and responding {hello.ProtoMessage}");

        ProtoHello response = new ProtoHello();
        response.ProtoMessage = $"howdy there from c# - you said {hello.ProtoMessage}";
        this.futureMessage = new ProtoMessage(response);

        return promise;
    }

    // Update is called once per frame
    void Update()
    {
        if(this.promised != null)
        {
            delay -= Time.deltaTime;
            if(delay < 0)
            {
                delay = 4.0f;
                this.promised.Resolve(this.futureMessage);
                this.promised = null;
                this.futureMessage = null;
            }
        }
    }
}
