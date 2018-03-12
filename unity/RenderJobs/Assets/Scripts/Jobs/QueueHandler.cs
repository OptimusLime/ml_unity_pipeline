using System.Collections;
using System.Collections.Generic;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using UnityEngine;
using System.Text;
using System.Linq;
using System;

namespace Jobs
{
    public class QueueHandler
    {

        //factory method for creating queue connections to rabbitMQ
        IConnection connection = null;

        // holds all our queue connections 
        Dictionary<string, IModel> channels = new Dictionary<string, IModel>();
        int currentCount = 0;
        // Use this for initialization
        public void ConnectQueues(string host = "localhost", int port = 5672, string[] queues = null)
        {

            if (this.connection == null)
            {
                var factory = new ConnectionFactory() { HostName = host, Port = port };
                this.connection = factory.CreateConnection();
            }

            foreach (var queueName in queues)
            {
                var channel = connection.CreateModel();
                
                channel.QueueDeclare(queue: queueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: null);

                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
                
                Debug.Log(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += (model, ea) =>
                {
                    this.currentCount += 1;
                    Debug.Log($"Concurrent handling {this.currentCount}");

                    // body of an incoming message
                    var body = ea.Body;

                    Debug.Log($"Body {body[0]}, more: {body[1]}");

                    // this will decode an incoming object into its component message
                    var protoMessage = MasterProtoRouter.Instance.DecodeByteToProto(body);

                    var props = ea.BasicProperties;


                    var replyProps = channel.CreateBasicProperties();
                    replyProps.CorrelationId = props.CorrelationId;
                    replyProps.ContentType = "plain/text";// "application/octet-stream";


                    // then we route through our global router, promising we'll
                    // eventually return
                    // this will process protoMessage in order of objects inside it
                    var protoPromise = MasterProtoRouter.Instance.RouteMessage(protoMessage);

                    protoPromise.Then(pMessage =>
                    {
                        Debug.Log($"Returning message");
                        Debug.Log($" {pMessage}");
                        byte[] rawBody = pMessage.GetBytes();
                        //var bodyString = Encoding.UTF8.GetString(rawBody, 0, rawBody.Length);
                        var replyChannel = props.ReplyTo != null ? props.ReplyTo : queueName;
                        //Debug.Log($"Sending response {replyChannel}, basics {replyProps.ContentType}");

                        // channel.BasicPublish(exchange: "", routingKey: replyChannel, basicProperties: 
                        channel.BasicPublish(exchange: "", routingKey: replyChannel,
                                             basicProperties: replyProps, body: rawBody);

                    //    Debug.Log($"ack the message");
                  //      throw new Exception("hot dogging it");
                        channel.BasicAck(deliveryTag: ea.DeliveryTag,
                          multiple: false);
                        
                    }).Catch(e =>
                    {
                        Debug.LogError($"ERROR ROUTING EVENTS {e}");

                    });
                };

                channel.BasicConsume(queue: queueName,
                                     autoAck: false,
                                     consumer: consumer);
                //save our queue connection
                channels[queueName] = channel;
            }
        }

        public void DisconnectQueues(string[] queues = null)
        {
            if (queues == null)
            {
                queues = channels.Keys.ToArray();
            }
            foreach (var queueName in queues)
                disconnectQueue(queueName);

        }
        private void disconnectQueue(string queueName)
        {
            //close and remove from our list of channels subscribed
            channels[queueName].Close();
            channels.Remove(queueName);
        }

        private void OnDestroy()
        {
            Debug.Log("destroying channel");
            this.DisconnectQueues();
        }
    }
}