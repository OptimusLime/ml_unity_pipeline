from pdb import set_trace as bb
# import asyncio
import json
import argparse
from datetime import datetime
from proto.models_pb2 import *
import struct
import pika
import uuid
import io
from PIL import Image
import numpy as np

global_map = {}
global_id = 1


def proto_join_to_dict(proto_join):
    proto_dict = {}
    # map ixToProtos into dictionary
    for cmap in proto_join.ixToProtos:
        class_ix = cmap.key
        Classname = globals()[cmap.value]
        proto_dict[Classname] = class_ix
    
    return proto_dict


# helper to go from byte string to np image
# https://stackoverflow.com/questions/42036890/how-to-decode-jpg-png-in-python
def screenshot_to_np(proto_ss):
    pixel_stream = io.BytesIO(proto_ss.data)
    return np.array(Image.open(pixel_stream))


class ProtoMessage():

    def __init__(self, class_mapping):
        # hold the message content
        self.msg_content = {'header': ProtoHeader(), 'body': ''.encode()}
        self.mapping = class_mapping

    # take in a proto class object
    # this will handle adding the proto bytes to the body
    # as well as mapping the appropriate info in the header
    def add_to_message(self, proto_obj):
        # grab the header
        msg_header = self.msg_content['header']

        # turn our proto object into
        proto_msg = proto_obj.SerializeToString()

        self.msg_content['body'] += proto_msg

        # no object, it's a join message
        proto_gid = 0
        proto_type_id = self.mapping[type(proto_obj)]

        # create new header item
        header_item = msg_header.protoItems.add()

        # create our header information uniquely identifying this object
        header_item.proto_id = proto_gid
        header_item.proto_type = proto_type_id
        header_item.proto_size = len(proto_msg)

        print("Adding body content len - {}, \n header - {}".format(
                len(proto_msg), header_item))

        # allow for message chaining
        return self

    def get_bytes(self):

        # serialize our header from the full msg we've constructed
        header_prepend = self.msg_content['header'].SerializeToString()
        
        print("Header len {}".format(len(header_prepend)))

        # let the header length be known
        packed_header_len = struct.pack('>L', len(header_prepend))

        # return the full object to be sent as byte array
        return packed_header_len + header_prepend + self.msg_content['body']


class MessageWrapper():

    def __init__(self):
        # go from type to ix
        self.expand_classes({ProtoContact: -1})

    def get_proto_class_map(self):
        proto_join = ProtoJoin()
        for proto_cls, proto_val in self.type_to_ix.items():
            ix_to_proto = proto_join.ixToProtos.add()

            ix_to_proto.key = proto_val
            ix_to_proto.value = str(proto_cls.__name__)
        return proto_join

    def join_message(self):
        # initial message is to get info about the server
        return (ProtoMessage(self.type_to_ix)
                .add_to_message(ProtoContact())
                .get_bytes())
    
    # agree on classes from a proto_join object
    def expand_classes(self, class_map):
        self.type_to_ix = {}
        
        for Classname, class_ix in class_map.items():
            self.type_to_ix[Classname] = class_ix
        
        self.ix_to_type = {val: k for k, val in self.type_to_ix.items()}

    # here we take in byte_data, then read the header and body from the data
    # A message should be full of multiple proto objects.
    # this function reads the header to determine how many, what type, and size
    # then it will decode each chunk into the objects present
    def decode_to_proto(self, byte_data):

        # first 4 bytes tell us about header
        header_len_bytes, head_plus_body = byte_data[:4], byte_data[4:]

        # decode a byte object into a collection of proto objects
        header_len = struct.unpack('>L', header_len_bytes)[0]

        # now get the header
        header_bytes = head_plus_body[:header_len]
        body_bytes = head_plus_body[header_len:]

        # now we can parse the header
        protoHeader = ProtoHeader()
        protoHeader.ParseFromString(header_bytes)

        body_offset = 0

        header_and_objects = []

        # now loop over our proto header
        for proto_item in protoHeader.protoItems:

            # header tells us about this item (just a few bytes per obj)
            gid = proto_item.proto_id
            obj_type_ix = proto_item.proto_type
            obj_len = proto_item.proto_size

            # slice our object bytes
            obj_bytes = body_bytes[body_offset:body_offset+obj_len]

            # move ahead our byte index
            body_offset += obj_len

            # get our class, this is what we'll decode from bytes
            # any time we don't know what class
            # we assume it's telling use about the classes
            ProtoClass = (self.ix_to_type[obj_type_ix]
                          if obj_type_ix in self.ix_to_type
                          else ProtoJoin)

            proto_object = ProtoClass()

            # convert our bytes into the object onces again
            proto_object.ParseFromString(obj_bytes)

            # record object to pass to router
            header_and_objects.append((proto_item, proto_object))       

        # decomposed our message into proto objects passed across the stream
        return header_and_objects


class BasicClient(object):
    def __init__(self, publish_queue):

        self.publish_queue = publish_queue
        self.connection = pika.BlockingConnection(pika.ConnectionParameters(
                host='localhost'))

        self.channel = self.connection.channel()

        result = self.channel.queue_declare(exclusive=True, durable=True)
        self.callback_queue = result.method.queue

        self.channel.basic_consume(self.on_response, no_ack=True,
                                   queue=self.callback_queue)
        self.msg_wrap = MessageWrapper()

    def on_response(self, ch, method, props, body):
        if self.corr_id == props.correlation_id:
            self.response = body
    
    # we send call and response
    def connect(self):

        # first message is to contact the server farm
        contact_message = self.msg_wrap.join_message()
        
        # get information about the classes we need to map to
        proto_res_header_and_obj = self.sync_call(contact_message)

        # get the first object in the array, 
        # adn then get the proto object (ignore the header)
        pjoin_obj = proto_res_header_and_obj[0][1]

        # use that to expand definition of class
        self.msg_wrap.expand_classes(proto_join_to_dict(pjoin_obj))

        # then we're done
        return self

    def get_message(self, *proto_args):
        proto_msg = ProtoMessage(self.msg_wrap.type_to_ix)

        # continually append the proto objects
        for proto_obj in proto_args:
            proto_msg = proto_msg.add_to_message(proto_obj)
        
        # then get our mesage
        return proto_msg.get_bytes()

    def sync_call(self, msg_body):
        self.response = None
        self.corr_id = str(uuid.uuid4())

        self.channel.basic_publish(exchange='',
                                   routing_key=self.publish_queue,
                                   properties=pika.BasicProperties(
                                         reply_to=self.callback_queue,
                                         correlation_id=self.corr_id,
                                         ),
                                   body=msg_body)
        while self.response is None:
            self.connection.process_data_events()
        
        return self.msg_wrap.decode_to_proto(self.response)

if __name__ == '__main__':

    import sys

    queue_name = 'task_queue'

    print("Getting class information from Unity")
    client = BasicClient(queue_name).connect()
    
    # lets say hello
    print("Class info achieved, saying hello")
    hello_friend = ProtoHello()
    hello_friend.proto_message = 'hi friend from python'

    # empty call to serve, wait for response
    cs_response = client.sync_call(client.get_message(hello_friend))
    print("simple call and respond {}".format(cs_response))

    ss_for_me = ProtoScreenShot()
    ss_for_me.width = 32
    ss_for_me.height = 32
    ss_response = client.sync_call(client.get_message(ss_for_me))
    np_ss = screenshot_to_np(ss_response[0][1])

    bb()
