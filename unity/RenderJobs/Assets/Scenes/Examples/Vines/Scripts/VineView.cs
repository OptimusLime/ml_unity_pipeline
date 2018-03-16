using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RSG;
using Jobs; 

public class VineView : BaseHandler {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public Promise<ProtoMessage> RenderVines(PCGVineView vines)
    {
        //
        return new Promise<ProtoMessage>((resolve, reject) =>
        {
            ProtoHello response = new ProtoHello();
            response.ProtoMessage = $"honk {vines.Vines.Count}";
            Debug.Log($"receiving vine and responsing");

            resolve(new ProtoMessage(response));
        });
    }

}
