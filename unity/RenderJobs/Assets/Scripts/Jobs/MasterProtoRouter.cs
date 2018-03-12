using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using RSG;

namespace Jobs
{

    public class MasterProtoRouter : Singleton<MasterProtoRouter>
    {
        bool protoClassMapped = false;
        Dictionary<int, Type> ixToClass = new Dictionary<int, Type>();
        Dictionary<Type, int> classToIx = new Dictionary<Type, int>();
        Dictionary<int, List<System.Tuple<MethodInfo, object>>> handlers = new Dictionary<int, List<System.Tuple<MethodInfo, object>>>();

        void setProtoMapping()
        {
            if (protoClassMapped)
                return;
            
            var protoTypes = System.AppDomain.CurrentDomain.GetAllProtoObjectTypes().ToList();
            protoTypes.Remove(typeof(ProtoContact));

            ixToClass.Clear();
            classToIx.Clear();

            // Contact is the base, and it's -1
            // everything else is in sorted order
            ixToClass.Add(-1, typeof(ProtoContact));
            classToIx.Add(typeof(ProtoContact), -1);

            for (var i = 0; i < protoTypes.Count; i++)
            {
                ixToClass.Add(i, protoTypes[i]);
                classToIx.Add(protoTypes[i], i);
            }

            Debug.Log($"Setting all relevant protos: {classToIx.Keys.Select(x => x.Name).ToArray()}");

            protoClassMapped = true;
        }


        protected MasterProtoRouter()
        {
            Debug.Log("Launching Master Router");


            //classToIx.Clear();
            //foreach (var kvp in ixToClass)
            //{
            //    classToIx.Add(kvp.Value, kvp.Key);
            //}

            //// what a hack, I just want to register a function for this proto
            //// maybe there is a better way, but this is the only that can match generic types
            //var methodInfo = this.GetType().GetMethod("AdjustSharedClasses", 
            //                                          new Type[]{typeof(ProtoJoin)});
            //print($"Method info {methodInfo}");

            
            ////register our response to hello messages :)
            //AddResponsePromise(typeof(ProtoJoin), this, methodInfo);

        } // guarantee this will be always a singleton only - can't use the constructor!
        void OnEnable()
        {
            this.RegisterProtoMessageHandlers();
        }
        void OnDisable()
        {
            this.RemoveProtoMessageHandlers();
        }
        public override void OnDestroy()
        {
            base.OnDestroy();
        }

        public int GetProtoTypeIx(object obj)
        {
            return this.GetProtoTypeIx(obj.GetType());
        }
        public int GetProtoTypeIx(Type protoType)
        {
            setProtoMapping();
            Debug.Log($" proto {protoType.Name} is in classToIx: {classToIx.ContainsKey(protoType)}");
            return classToIx[protoType];
        }

        public Promise<ProtoMessage> FirstContactMessage(ProtoContact protoContact)
        {
            // send back a protojoin message with all of our class information (as it currently is)
            return new Promise<ProtoMessage>((resolve, reject) =>
            {
                var protoJoin = new ProtoJoin();

                foreach(var ixAndClass in this.ixToClass)
                {
                    var mapClass = new ProtoMapping()
                    {
                        Key = ixAndClass.Key,
                        Value = ixAndClass.Value.Name
                    };
                    //mapClass.Key = ixAndClass.Key;
                    //mapClass.Value = ixAndClass.Value;
                    protoJoin.IxToProtos.Add(mapClass);
                }

                // respond with the current mapping of all proto objects in the project
                resolve(new ProtoMessage(protoJoin));
            });
        }

        public Promise<ProtoMessage> AdjustSharedClasses(ProtoJoin protoJoin)
        {

            Debug.Log($"Adjusting Protojoin {protoJoin}");
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

        public void AddResponsePromise(Type protoType, object handler, MethodInfo method)
        {
            // first ensure that we know about all our classes
            int objIx = this.GetProtoTypeIx(protoType);

            // have we handled this before? 
            if (!this.handlers.ContainsKey(objIx))
                this.handlers.Add(objIx, new List<System.Tuple<MethodInfo, object>>());

            //going to add a response tuple to our object
            var toAdd = new System.Tuple<MethodInfo, object>(method, handler);
           
            //add unique tuple if it doesn't already exit 
            if (!this.handlers[objIx].Contains(toAdd))
            {
                this.handlers[objIx].Add(toAdd);
                Debug.Log($"Now with {this.handlers[objIx].Count} handlers for {protoType.Name}");
            }
            else
                Debug.Log($"Attempted double handle for {protoType.Name} with handler: {handler}");

        }

        public void RemoveResponse(Type protoType, object handler, MethodInfo method)
        {
            int objIx = this.GetProtoTypeIx(protoType);

            //going to add a response tuple to our object
            var toRemove = new System.Tuple<MethodInfo, object>(method, handler);

            if (this.handlers[objIx].Remove(toRemove))
                Debug.Log($"Removed message handler {handler.GetType()}");
        }

        IPromise<ProtoMessage> mergePromise(IEnumerable<ProtoMessage> partialMessages)
        {
            var promise = new Promise<ProtoMessage>((resolve, reject) =>
            {
                var fullMessage = new ProtoMessage();
                foreach (var pm in partialMessages)
                    if (pm != null)
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

            var allPromises = new List<Promise<ProtoMessage>>();

            // we're going to loop over each message sent 
            // and for each handler of those objects
            // processing each partial response in order 
            // and sending back the full response in the end
            Debug.Log($"Objects to route:  {protoMessage.ProtoObjectList.Count}");
            foreach (var msgTuple in protoMessage.ProtoObjectList)
            {
                var objIx = msgTuple.Item1.ProtoType;
                var objHandlers = this.handlers[objIx];

                Debug.Log($"Obj Type {objIx}, handler count {objHandlers.Count}");

                foreach (var methodAndObj in objHandlers)
                {
                    var methodToCall = methodAndObj.Item1;
                    var objToCall = methodAndObj.Item2;

                    Debug.Log($"Promising to send the object to {objToCall.GetType().Name}");
                    allPromises.Add((Promise<ProtoMessage>)methodToCall.Invoke(objToCall, new object[] { msgTuple.Item2 }));
                }
            }

            //here we guarantee to return eventually -- so this can be async handled
            //first fullfill all of our promises,
            //then take the collection of results and merge them into a single return message
            return Promise<ProtoMessage>
                .All(allPromises)
                .Then(protoCollection =>
                {
                    Debug.Log("All messages routed and responded to");
                    return mergePromise(protoCollection);
                });
        }

        // Here we simply decode our byte buffer into a collection of proto objects
        // then we return a holder of all the proto objects, to be processed elsewhere (the router)
        public ProtoMessage DecodeByteToProto(byte[] byteArray)
        {
            ProtoMessage incomingMessage = new ProtoMessage();

            ByteOperations.ByteReader byteBuffer = new ByteOperations.ByteReader(byteArray);

            try
            {

                Debug.Log($"littel end {BitConverter.IsLittleEndian}");
                //get info about the header
                Int32 headerLength = byteBuffer.ReadInt();

                
                Debug.Log($"Reading header of size: {headerLength}, total size: {byteBuffer.Length}");
                var header = byteBuffer.ReadBytes(headerLength);

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
                    var nextObjectBytes = byteBuffer.ReadBytes(readLength);

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
            Debug.Log("Finished deconstructing message");
            return incomingMessage;
        }


    }
}