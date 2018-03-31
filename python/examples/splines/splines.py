from unity_connect import BasicClient, screenshot_to_np
from IPython import embed
from proto.examples_pb2 import ProtoSpline, ProtoV3
from proto.models_pb2 import ProtoScreenShot
import numpy as np


# queue up
queue_name = "task_queue"

# connect
client = BasicClient(queue_name).connect()


# assign random x,y,z
def random_point(p, radius):
    p.x = radius*np.random.uniform(-1, 1)
    p.y = radius*np.random.uniform(-1, 1)
    p.z = radius*np.random.uniform(-1, 1)


# send a spline over to be rendered
def send_random_spline(width, height, radius, points):
    # create our spline object
    ps = ProtoSpline()
    ps.screen_width = width
    ps.screen_height = height

    for i in range(points):
        # add to our control points, off we go
        p = ps.control_points.add()

        # set to random 
        random_point(p, radius)
    
    ss = ProtoScreenShot()
    ss.width = width
    ss.height = height

    # send out to client please
    cs_response = client.sync_call(client.get_message(ps, ss))
    np_ss = screenshot_to_np(cs_response[1][1])
    return np_ss

ss = send_random_spline(100, 100, 5, 3)
embed()

