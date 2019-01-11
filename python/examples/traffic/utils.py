import json
from proto.models_pb2 import ProtoScreenShot
from unity_connect import screenshot_to_np
import torch
from pdb import set_trace as bb

# send a spline over to be rendered
def unity_render_traffic_scene(client, traffic_lights, width, height):

    # get a screenshot as well
    ss = ProtoScreenShot()
    ss.width = width
    ss.height = height

    # send out to client please
    cs_response = client.sync_call(client.get_message(traffic_lights, ss))
    scene_info = cs_response[0]
    np_ss = screenshot_to_np(cs_response[1][1])
    return scene_info, np_ss


def render(client, struct, width, height):

    # numpy version of image coming back
    scene_info, np_img = unity_render_traffic_scene(client, struct, width, height)

    # send back a pytorch version of the image
    # wrapped with handler for the similarity fct
    return scene_info, torch.from_numpy(np_img)


def scene_to_json(scene, fname='scenes.json'):
    with open(fname, 'a+') as f:
        json.dump(scene, f)
