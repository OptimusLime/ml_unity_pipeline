using UnityEngine;
using System.Collections;
using System.Reflection;
using Jobs;
using RSG;
using System;

public class DelayHelloHandler : BaseHandler
{

    ProtoMessage futureMessage = null;
    Promise<ProtoMessage> promised = null;
    float delay = 4.0f;
    System.Random r = new System.Random();

    // Use this for initialization
    void Start(){
        //this.RegisterProtoMessageHandlers();
    //{
        //// what a hack, I just want to register a function for this proto
        //// maybe there is a better way, but this is the only that can match generic types
        //var methodInfo = this.GetType().GetMethod("ReceiveHello", new Type[]{typeof(ProtoHello)});

        ////register our response to hello messages :)
        //MasterProtoRouter.Instance.AddResponsePromise(typeof(ProtoHello), this, methodInfo);


    }


    //Handle incoming hellos -- and send a response when we're done
    public Promise<ProtoMessage> ReceiveHello(ProtoHello hello)
    {
        var promise = new Promise<ProtoMessage>();
        delay = (float)r.NextDouble()*4;
        this.promised = promise;
        Debug.Log($"receiving a friendly hello and responding in future to msg {hello.ProtoMessage}");

        ProtoHello response = new ProtoHello();
        response.ProtoMessage = $"delayed {delay}s howdy from c# - you said: {hello.ProtoMessage}";
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
                Debug.Log("Resolving delayed process :)");
                delay = (float)r.NextDouble()*4;
                this.promised.Resolve(this.futureMessage);
                this.promised = null;
                this.futureMessage = null;
            }
        }
    }

    //void OnDisable()
    //{
    //    this.RemoveProtoMessageHandlers();
    //}
}
