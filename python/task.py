from pdb import set_trace as bb
import asyncio
import json
import argparse
from datetime import datetime
from proto.models_pb2 import ProtoHeader, ProtoMapping, ProtoJoin, ProtoHello
import struct
from pika import BlockingConnection, ConnectionParameters

global_map = {}
global_id = 1


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

        # allow for message chaining
        return self

    def get_bytes(self):

        # serialize our header from the full msg we've constructed
        header_prepend = self.msg_content['header'].SerializeToString()

        # let the header length be known
        packed_header_len = struct.pack('>L', len(header_prepend))

        # return the full object to be sent as byte array
        return packed_header_len + header_prepend + self.msg_content['body']


class MessageWrapper():

    def __init__(self, connections, users):
        self.connections = connections
        self.users = users
        self.peername = ""
        self.user = None

        # go from type to ix
        self.type_to_ix = {ProtoHeader: 0, ProtoMapping: 1,
                           ProtoJoin: 2, ProtoHello: 3,
                           ProtoVector2: 4, BuildMazeMsg: 5}

        self.ix_to_type = {val: k for k, val in self.type_to_ix.items()}

    # def get_full_mapping_proto(self):
    #     proto_join = ProtoJoin()
    #     for proto_cls, proto_val in self.type_to_ix.items():
    #         ix_to_proto = proto_join.ixToProtos.add()

    #         ix_to_proto.key = proto_val
    #         ix_to_proto.value = str(proto_cls.__name__)
    #     return proto_join

    def join_message(self):

        # create a message with multiple proto objects inside
        join_msg_holder = ProtoMessage(self.type_to_ix)

        # get our full mapping of types!
        proto_map = self.get_full_mapping_proto()

        pt_hello = ProtoHello()
        pt_hello.proto_message = "hi ya from python doofus"

        # send a map and a hello :)
        return (join_msg_holder
                .add_to_message(proto_map)
                .add_to_message(pt_hello)
                .get_bytes())

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
            ProtoClass = self.ix_to_type[obj_type_ix]
            proto_object = ProtoClass()

            # convert our bytes into the object onces again
            proto_object.ParseFromString(obj_bytes)

            # record this object to pass to our router
            header_and_objects.append((proto_item, proto_object))

        # decomposed our message into proto objects passed across the stream
        return header_and_objects


if __name__ == '__main__':

    import pika
    import sys

    connection = BlockingConnection(ConnectionParameters(host='localhost'))
    channel = connection.channel()

    channel.queue_declare(queue='task_queue', durable=True)

    # message = ' '.join(sys.argv[1:]) or "Hello World!"
    channel.basic_publish(exchange='',
                          routing_key='task_queue',
                          body=MessageWrapper().join_message(),
                          # make message persistent
                          properties=pika.BasicProperties(delivery_mode=2))
    
    print(" [x] Sent %r" % message)
    connection.close()
