using UnityEngine;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using RSG;

namespace Jobs
{

    public class ProtoRouter : Singleton<ProtoRouter>
    {
        Dictionary<int, Type> ixToClass = new Dictionary<int, Type>()
            { { 0, typeof(ProtoHeader) },
            { 1, typeof(ProtoMapping) },
            { 2, typeof(ProtoJoin) },
            { 3, typeof(ProtoHello)}
        };
        Dictionary<Type, int> classToIx = new Dictionary<Type, int>();
        Dictionary<int, List<System.Tuple<MethodInfo, object>>> handlers = new Dictionary<int, List<System.Tuple<MethodInfo, object>>>();

        protected ProtoRouter() {
        
            classToIx.Clear();
            foreach (var kvp in ixToClass)
            {
                classToIx.Add(kvp.Value, kvp.Key);
            }

        } // guarantee this will be always a singleton only - can't use the constructor!


        public int GetProtoType(object obj)
        {
            return classToIx[obj.GetType()];
        }

        Promise<ProtoMessage> AdjustSharedClasses(ProtoJoin protoJoin)
        {
            return new Promise<ProtoMessage>((resolve, reject) =>
            {
                //got a bunch of mappings
                var mappings = protoJoin.IxToProtos;

                //look over the mapping from index to key
                foreach (var pItem in mappings)
                {
                    var pType = Type.GetType(pItem.Value);
                    if (!ixToClass.ContainsKey(pItem.Key))
                    {
                        ixToClass.Add(pItem.Key, pType);
                        classToIx.Add(pType, pItem.Key);
                    }
                    else
                    {
                        Debug.Log($"ignoring proto mapping {pItem}, alread have {ixToClass[pItem.Key]}");
                    }
                }

                //nothing to add to the response
                resolve(null);
            
            });
        }

        

        //void handleNoIDObject(object protoObject)
        //{
        //    switch (classToIx[protoObject.GetType()])
        //    {
        //        //ProtoJoin static number
        //        case 2:
        //            addSharedProtoClasses((ProtoJoin)protoObject);
        //            break;
        //        default:
        //            throw new NotImplementedException($"Don't know how to handle {protoObject} with class int {classToIx[protoObject.GetType()]} of type {protoObject.GetType()}");
        //    }

        //}

        public void AddResponsePromise(Type objType, object handler, MethodInfo method)
        {
            int objIx = classToIx[objType];
            if (!this.handlers.ContainsKey(objIx))
                this.handlers.Add(objIx, new List<System.Tuple<MethodInfo, object>>());
            this.handlers[objIx].Add(new System.Tuple<MethodInfo, object>(method, handler));
        }

        IPromise<ProtoMessage> mergePromise(IEnumerable<ProtoMessage> partialMessages)
        {
            var promise = new Promise<ProtoMessage>((resolve, reject) => {
                var fullMessage = new ProtoMessage();
                foreach(var pm in partialMessages)
                    if(pm != null)
                        fullMessage.AddProtoObjects(pm);

                resolve(fullMessage);
            });

            return promise;
        }

        //most complicated logic we have. Need to loop over the routing objects and handle
        //objects coming in the stream in order
        //therefore we construct a series of promises
        public IPromise<ProtoMessage> RouteMessage(ProtoMessage protoMessage)
        {
            //here we guarantee to return eventually -- so this can be async handled
            var promise = new Promise<ProtoMessage>();

            var allPromises = new List<IPromise<ProtoMessage>>();

            // we're going to loop over each message sent 
            // and for each handler of those objects
            // processing each partial response in order 
            // and sending back the full response in the end
            foreach(var msgTuple in protoMessage.ProtoObjectList)
            {   
                var objIx = msgTuple.Item1.ProtoType;
                var objHandlers = this.handlers[objIx];
                foreach(var methodAndObj in objHandlers)
                {

                    var methodToCall = methodAndObj.Item1;
                    var objToCall = methodAndObj.Item2;

                    allPromises.Add((Promise<ProtoMessage>) methodToCall.Invoke(objToCall, new object[] { msgTuple.Item2 }));
                }
            }

            promise.ThenAll(result => allPromises).Then(protoCollection => {
                return mergePromise(protoCollection);
            });
                            
            return promise;
        }

        // Here we simply decode our byte buffer into a collection of proto objects
        // then we return a holder of all the proto objects, to be processed elsewhere (the router)
        public ProtoMessage DecodeByteToProto(byte[] byteBuffer)
        {
            ProtoMessage incomingMessage = new ProtoMessage();

            try
            {

                //get info about the header
                int currentRead = 0;
                Int32 headerLength = BitConverter.ToInt32(byteBuffer, currentRead);
                currentRead += sizeof(Int32);


                Debug.Log($"Reading header of size: {headerLength}, total size: {byteBuffer.Length - currentRead}");
                var header = byteBuffer.Skip(currentRead).Take(headerLength);
                currentRead += headerLength;

                // parse out the header information from the above clue 
                var protoHeader = ProtoHeader.Parser.ParseFrom(header.ToArray());

                Debug.Log($"header len: {headerLength}, 'val' : {protoHeader}");

                // now we have a list of objects in our header, and we can ready each length
                foreach (var protoItem in protoHeader.ProtoItems)
                {
                    var pid = protoItem.ProtoId;
                    var pType = protoItem.ProtoType;
                    var readLength = protoItem.ProtoSize;

                    Debug.Log($"Reading next item: obj id: {pid} type: {pType}, length: {readLength}");
                    //get our next object bytes according to sizes
                    var nextObjectBytes = byteBuffer.Skip(currentRead).Take(readLength).ToArray();
                    currentRead += readLength;

                    //use our type info to get the static parser and parse 
                    //from the bytes we just read

                    //get the static parser property
                    var parserInfo = ixToClass[pType].GetProperty("Parser", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                    Debug.Log($"Reading next item: type: {ixToClass[pType]}");
                    Debug.Log($"Reading next item parser: {ixToClass[pType].GetProperty("Parser", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)}");

                    //get the ParseFrom internal method in the protobuf message class
                    var parseStringMethod = parserInfo.PropertyType.GetMethod("ParseFrom", new[] { typeof(byte[]) });
                    Debug.Log($"Reading next item parse method: {parseStringMethod}");

                    //get the static parser
                    object parser = parserInfo.GetValue(null, null);

                    //now actuall parse from the bytes 
                    var obj = parseStringMethod.Invoke(parser, new object[] { nextObjectBytes });
                    var objType = obj.GetType();

                    incomingMessage.AddProtoObject(protoItem, obj);

                    Debug.Log($"Finished item #{pid}, type: {pType}, length: {readLength}, parsed obj: {obj}");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"exception reading {e}");
                Debug.Log($"exception reading ST: {e.StackTrace}");
            }

            // send back the message with all the proto objects inside
            return incomingMessage;
        }


    }
}