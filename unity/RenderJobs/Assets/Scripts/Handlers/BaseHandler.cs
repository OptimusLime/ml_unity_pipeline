using UnityEngine;
using System.Collections;

public class BaseHandler : MonoBehaviour
{
    protected void OnEnable()
    {
        this.RegisterProtoMessageHandlers();
    }
    protected void OnDisable()
    {
        this.RemoveProtoMessageHandlers();
    }

}
