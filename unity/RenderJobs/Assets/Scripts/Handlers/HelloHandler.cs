using UnityEngine;
using System.Collections;
using System.Reflection;
using Jobs;
using RSG;
using System;


public class HelloHandler : BaseHandler
{
    

	//// Use this for initialization
	//void Start()
	//{
 //       this.RegisterProtoMessageHandlers();
 //       //// what a hack, I just want to register a function for this proto
 //       //// maybe there is a better way, but this is the only that can match generic types
 //       //var methodInfo = this.GetType().GetMethod("ReceiveHello");

 //       ////register our response to hello messages :)
 //       //MasterProtoRouter.Instance.AddResponsePromise(typeof(ProtoHello), this, methodInfo);
	//}


    //Handle incoming hellos -- and send a response when we're done
    public Promise<ProtoMessage> ReceiveHello(ProtoHello hello)
    {
        return new Promise<ProtoMessage>((resolve, reject) =>
        {
            ProtoHello response = new ProtoHello();
            response.ProtoMessage = $"howdy there from c# - you said: {hello.ProtoMessage}";
            Debug.Log($"receiving a friendly hello and responding to msg {hello.ProtoMessage} with {response}");

            resolve(new ProtoMessage(response));
        });
    }

	// Update is called once per frame
	void Update()
	{
			
	}

    //void OnDisable()
    //{
    //    this.RemoveProtoMessageHandlers();
    //}
}
