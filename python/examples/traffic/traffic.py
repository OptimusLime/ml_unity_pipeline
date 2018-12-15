import os
import sys
sys.path.append(os.path.join(os.path.dirname(__file__),  "../../"))  # noqa
import torch
import pyro
import pyro.distributions as dist
import numpy as np
from unity_connect import BasicClient, screenshot_to_np
from proto.traffic_pb2 import ProtoTrafficScene, ProtoSceneEnvironment, ProtoVector3
from proto.models_pb2 import ProtoScreenShot
from pdb import set_trace as bb
import visdom
from math import sqrt

vv = visdom.Visdom(env="dope")

# queue up
queue_name = "task_queue"

# connect
client = BasicClient(queue_name).connect()


# send a spline over to be rendered
def unity_render_traffic_scene(traffic_lights, width, height):

    # get a screenshot as well
    ss = ProtoScreenShot()
    ss.width = width
    ss.height = height

    # send out to client please
    cs_response = client.sync_call(client.get_message(traffic_lights, ss))
    np_ss = screenshot_to_np(cs_response[1][1])
    return np_ss


def render(struct, width, height):

    # numpy version of image coming back
    np_img = unity_render_traffic_scene(struct, width, height)

    # send back a pytorch version of the image
    # wrapped with handler for the similarity fct
    return torch.from_numpy(np_img)


def model(width, height):
    # create a default scene
    scene = ProtoTrafficScene()
    scene.scene_name = "demo"
    scene.environment.hour_of_day = 12
    scene.environment.weather = "clear"
    scene.screen_width = width
    scene.screen_height = height

    # add a single light
    pt = scene.traffic_lights.add()
    pt.light_type = 1
    probs = torch.Tensor([1., 1., 1.])
    probs = probs/probs.sum()
    pt.light_status = pyro.sample("status", dist.Categorical(probs))
    pt.location.x = pyro.sample("x", dist.Normal(0., 3.))
    pt.location.y = pyro.sample("y", dist.Normal(0., 3.))
    pt.location.z = pyro.sample("z", dist.Normal(0., 3.))

    pt.orientation.x = -90  # + pyro.sample("noise-xorient", dist.Normal(0., 1.))
    pt.orientation.y = 180 + pyro.sample("noise-xorient", dist.Normal(0., 30.))
    pt.orientation.z = 0

    return scene


def view_images(imgs):
    all_imgs = imgs.unsqueeze(0).permute([0, 3, 1, 2])
    vv.images(all_imgs.data.numpy(), nrow=int(sqrt(len(imgs))))


def forward_and_view(width, height):
    for i in range(100):
        scene = model(width, height)
        imgs = render(scene, width, height)
        view_images(imgs)


#
width, height = 1024, 512

#
forward_and_view(width, height)



