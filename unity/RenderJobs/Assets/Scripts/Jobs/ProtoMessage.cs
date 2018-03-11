using UnityEngine;
using System.Runtime;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using Google.Protobuf;
using System.Reflection;

namespace Jobs
{
    public class ProtoMessage
    {
        private int mHeaderSize = 0;
        private ProtoHeader mHeader = new ProtoHeader();
        private int mCurrentOffset = 0;
        private List<byte> mBody;

        private List<Tuple<ItemHeader, object>> protoObjects;

        public List<Tuple<ItemHeader, object>> ProtoObjectList
        {
            get { return this.protoObjects; }
        }

        public ProtoMessage(List<Tuple<ItemHeader, object>> protoObjects)
        {
            this.protoObjects = protoObjects;
        }
        public ProtoMessage()
        {
            this.protoObjects = new List<Tuple<ItemHeader, object>>();
        }
        public ProtoMessage(Tuple<ItemHeader, object> message)
            : this(new List<Tuple<ItemHeader, object>> { message })
        {
        }
        public ProtoMessage(object protoObject) : this()
        {
            this.AddProtoObject(protoObject);
        }
        //merge in another message
        public ProtoMessage AddProtoObjects(ProtoMessage other)
        {
            this.AddProtoObjects(other.ProtoObjectList);
            return this;
        }
        public ProtoMessage AddProtoObject(object protoObject)
        {
            var header = new ItemHeader()
            {
                ProtoId = -1,
                ProtoSize = -1,
                ProtoType = ProtoRouter.Instance.GetProtoType(protoObject)
            };

            this.AddProtoObject(header, protoObject);
            return this;
        }
        public ProtoMessage AddProtoObject(ItemHeader header, object protoObject)
        {
            this.protoObjects.Add(new Tuple<ItemHeader, object>(header, protoObject));
            return this;
        }

        public ProtoMessage AddProtoObjects(List<Tuple<ItemHeader, object>> protoObjects)
        {
            this.protoObjects.AddRange(protoObjects);
            return this;
        }

        void protoToByte(Tuple<ItemHeader, object> headerAndObject)
        {
            var obj = headerAndObject.Item2;

            // all protobuf objects have ToByteArray methods that take no params
            MethodInfo methodInfo = obj.GetType().GetMethod("ToByteArray");

            try
            {
                byte[] byteEncoded = (byte[]) methodInfo.Invoke(obj, null);
                this.AddProtoObject(byteEncoded, headerAndObject.Item1.ProtoType);
            }
            catch(Exception e)
            {
                Debug.LogError($"Cannot encode message. Hard exit {e}.");
                throw e;
            }
        }

        public byte[] GetBytes()
        {
            //reset our index
            mCurrentOffset = 0;
            mHeaderSize = 0;

            //convert all our items into bytes
            //TODO: Only call this once unless modified in between
            foreach (var headerAndObject in protoObjects)
            {
                protoToByte(headerAndObject);
            }

            //convert to our byte format for header 
            var headerBytes = mHeader.ToByteArray();

            //now we know total length
            var totalByteLength = sizeof(Int32) + headerBytes.Length + mCurrentOffset;
            var fullBuffer = new byte[totalByteLength]; //# Unpooled.Buffer(totalByteLength);

            //write our header size
            int currentWrite = 0;
            int headerLength = headerBytes.Length;
            Buffer.BlockCopy(BitConverter.GetBytes(headerLength), 0, fullBuffer, currentWrite, sizeof(int));
            currentWrite += sizeof(int);

            //write our header 
            Buffer.BlockCopy(headerBytes, 0, fullBuffer, currentWrite, headerBytes.Length);
            currentWrite += headerBytes.Length;

            //then write the content of the message
            Buffer.BlockCopy(mBody.ToArray(), 0, fullBuffer, currentWrite, mBody.Count);
            currentWrite += mBody.Count;

            //send back the buffer full of bytes, not so hard
            return fullBuffer;
        }

        ProtoMessage AddProtoObject(byte[] protoBytes, int protoType)
        {
            Debug.Assert(protoBytes.Length > 0, "Proto objects must have non-zero byte arrays for message passing");

            // when first constructed, 
            if (mCurrentOffset == 0)
                mBody = new List<byte>();

            mBody.AddRange(protoBytes);
            mCurrentOffset += protoBytes.Length;

            mHeader.ProtoItems.Add(new ItemHeader()
            {
                ProtoId = 0,
                ProtoSize = protoBytes.Length,
                ProtoType = protoType
            });

            return this;
        }

    }

}