import os
import argparse
import sys
sys.path.append(os.path.join(os.path.dirname(__file__),  "../../"))  # noqa
import torch
import pyro
import pyro.distributions as dist
import numpy as np
from unity_connect import BasicClient
from proto.traffic_pb2 import ProtoTrafficScene, ProtoSceneEnvironment, ProtoVector3, ProtoWeatherEnum
from pdb import set_trace as bb
import visdom
import json
from math import sqrt
from utils import render, scene_to_json
from google.protobuf.json_format import MessageToJson

vv = visdom.Visdom(env="dope")

# queue up
queue_name = "task_queue"


def model(width, height):
    # create a default scene
    scene = ProtoTrafficScene()
    scene.scene_name = "demo"
    scene_graph = {}

    scene.environment.hour_of_day = pyro.sample("hour_of_day",
                                                dist.Uniform(0., 24.))
    scene_graph['time'] = scene.environment.hour_of_day

    w_probs = torch.ones(len(ProtoWeatherEnum.keys()))
    w_probs[7] = 0.0
    w_probs = w_probs/w_probs.sum()
    # ('CLEAR', 0),
    # ('CLOUDY_1', 1),
    # ('CLOUDY_2', 2),
    # ('CLOUDY_3', 3),
    # ('CLOUDY_4', 4),
    # ('FOGGY', 5),
    # ('LIGHT_RAIN', 6),
    # ('HEAVY_RAIN', 7), <- this sky looks red, skip
    # ('LIGHT_SNOW', 8),
    # ('HEAVY_SNOW', 9),
    # ('STORM', 10)
    weather_ix = pyro.sample("weather", dist.Categorical(w_probs))
    scene_graph['weather'] = weather_ix.item()

    scene.environment.weather = ProtoWeatherEnum.values()[weather_ix]
    # scene.environment.weather_label = ProtoWeatherEnum.keys()[weather_ix]

    scene.screen_width = width
    scene.screen_height = height
#     scene.wait_time = 1.0
    scene.wait_time = 0.5

    num_lights = pyro.sample('num_objects', dist.Poisson(4.))
    scene_graph['num_lights'] = num_lights.item()
    objects = []
    for _ in range(int(num_lights)):
        pt = scene.traffic_lights.add()
        pt.light_type = 1
        # equal probability of light status
        probs = torch.Tensor([2., 1., 2.])
        probs = probs/probs.sum()
        pt.light_status = pyro.sample("status", dist.Categorical(probs))

        pt.location.x = pyro.sample("x", dist.Normal(0., 5.))
        pt.location.y = pyro.sample("y", dist.Normal(7, 3.))
        pt.location.z = pyro.sample("z", dist.Normal(7., 3.))

        pt.orientation.x = -90  # + pyro.sample("noise-xorient", dist.Normal(0., 1.))
        # only vary azimuth angle
        pt.orientation.y = 180 + pyro.sample("noise-xorient", dist.Normal(0., 30.))
        pt.orientation.z = 0

        # add to sg dict
        light = {'type': pt.light_type,
                 'status': pt.light_status,
                 'location': [pt.location.x, pt.location.y, pt.location.z],
                 'orientation': [pt.orientation.x, pt.orientation.y, pt.orientation.z],
                 }
        objects.append(light)
    scene_graph['objects'] = objects

    return scene, scene_graph


def view_images(imgs):
    all_imgs = imgs.unsqueeze(0).permute([0, 3, 1, 2])
    vv.images(all_imgs.data.numpy(), nrow=int(sqrt(len(imgs))))


def save_images(imgs, name):
    import scipy.misc
    scipy.misc.imsave('images/{}.jpg'.format(name), imgs)


def forward_and_view(args):
    client = BasicClient(queue_name).connect()
    print('Connected to unity!')
    scene_mds = []
    for i in range(args.num_samples):
        scene, md = model(args.width, args.height)
        scene_mds.append(md)
        print(md)
        scene_info, imgs = render(client, scene, args.width, args.height)
        print(scene_info)
        view_images(imgs)
        if args.save:
            save_images(imgs, str(i))
    if args.save:
        scene_to_json(scene_mds)

if __name__ == '__main__':
    parser = argparse.ArgumentParser(description="parse args")
    parser.add_argument('-n', '--num-samples', default=2, type=int)
    parser.add_argument('-w', '--width', default=1024, type=int)
    parser.add_argument('--height', default=512, type=int)
    parser.add_argument('--save', action='store_true')
    args = parser.parse_args()
    forward_and_view(args)
