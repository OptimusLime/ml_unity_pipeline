import os
import sys
sys.path.append(os.path.join(os.path.dirname(__file__),  "../../"))  # noqa
import torch
import pyro
import pyro.distributions as dist
import numpy as np
from unity_connect import BasicClient, screenshot_to_np
from proto.sprites_pb2 import ProtoBody
from proto.models_pb2 import ProtoScreenShot
from pdb import set_trace as bb
import visdom
from math import sqrt
import itertools

vv = visdom.Visdom(env="sprites")

# queue up
queue_name = "task_queue"

# connect
client = BasicClient(queue_name).connect()

all_body_parts = {
    "Eyebrows": {
        "thin_black": 0,
        "thin_dark_brown": 1,
        "thin_brown": 2,
        "thin_tope": 3,
        "thin_red": 4,
        "thin_gray": 5,
        "thin_white": 6,
        "thin_dark_gray": 7,
        "surprise_brown": 8,
        "surprise_tope": 9,
        "surprise_gray": 10,
        "surprise_red": 11,
        "surprise_white": 12,
        "surprise_dark_gray": 13,
        "surprise_dark_brown": 14,
        "surprise_black": 15,
        "thick_dark_gray": 16,
        "thick_dark_brown": 17,
        "thick_white": 18,
        "thick_brown": 19,
        "thick_black": 20,
        "thick_red": 21,
        "thick_gray": 22,
        "thick_tope": 23,
        "super_thin_black": 24,
    },
    "Eyes": {
        "thin_black": 0,
        "thin_dark_brown": 1,
        "thin_brown": 2,
        "thin_tope": 3,
        "thin_red": 4,
        "thin_gray": 5,
        "thin_white": 6,
        "thin_dark_gray": 7,
        "surprise_brown": 8,
        "surprise_tope": 9,
    },
    "Nose": {

    },
    "Mouth": {

    },
    "Arm right": {
        "bare_light_to_dark_1": 0,
        "bare_light_to_dark_2": 1,
        "bare_light_to_dark_3": 2,
        "bare_light_to_dark_4": 3,
        "bare_light_to_dark_5": 4,
        "bare_light_to_dark_6": 5,
        "bare_light_to_dark_7": 6,
        "bare_light_to_dark_8": 7,
        "sleeve_dark_blue": 8,
        "sleeve_teal": 9,
        "sleeve_green": 10,
        "sleeve_light_blue": 11,
        "sleeve_gray": 12,
        "sleeve_white": 13,
        "sleeve_orange": 14,
        "sleeve_red": 15
    },
    "Hand right": {

    },
    "Leg right": {

    },
    "Shoe right": {

    },
    "Hair": {

    },
    "Face": {

    },
    "Shirt": {

    },
    "Pants": {

    },
    "Arm left": {
        "bare_light_to_dark_1": 0,
        "bare_light_to_dark_2": 1,
        "bare_light_to_dark_3": 2,
        "bare_light_to_dark_4": 3,
        "bare_light_to_dark_5": 4,
        "bare_light_to_dark_6": 5,
        "bare_light_to_dark_7": 6,
        "bare_light_to_dark_8": 7,
        "sleeve_dark_blue": 8,
        "sleeve_teal": 9,
        "sleeve_green": 10,
        "sleeve_light_blue": 11,
        "sleeve_gray": 12,
        "sleeve_white": 13,
        "sleeve_orange": 14,
        "sleeve_red": 15
    },
    "Hand left": {

    },
    "Leg left": {

    },
    "Shoe left": {

    },

}


# send a spline over to be rendered
def unity_render_oed_sprites(traffic_lights, width, height, output_file=None):

    # get a screenshot as well
    ss = ProtoScreenShot()
    ss.width = width
    ss.height = height

    # send out to client please
    cs_response = client.sync_call(client.get_message(traffic_lights, ss))
    img_bytes = cs_response[1][1]

    if output_file is not None:
        with open(output_file, 'wb') as f:
            f.write(img_bytes.data)

    np_ss = screenshot_to_np(img_bytes)
    return np_ss


def render(struct, width, height, output_file=None):

    # numpy version of image coming back
    np_img = unity_render_oed_sprites(struct, width, height, output_file=output_file)

    # send back a pytorch version of the image
    # wrapped with handler for the similarity fct
    return torch.from_numpy(np_img)


def create_body_scene(width, height, parts_and_ix):
    # create a default scene
    body = ProtoBody()
    body.screen_width = width
    body.screen_height = height

    # create my body parts
    for part_name, choice_label, ix in parts_and_ix:
        bp = body.body_parts.add()
        bp.part_name = part_name
        bp.body_id = ix
        if part_name == "Arm right":
            bp = body.body_parts.add()
            bp.part_name = "Arm left"
            bp.body_id = ix

    return body


def cartesian_product(*arrays):
    return list(itertools.product(*arrays))


# loop over all the parts we care about
def enumerate_body_types(part_names):
    # get our potential body parts
    all_options = [[(p_name, p_label, p_ix)
                    for p_label, p_ix in all_body_parts[p_name].items()]
                   for p_name in part_names]

    # then we do a cartesian to get the actual combos
    return cartesian_product(*all_options)


def view_images(imgs):
    all_imgs = imgs.unsqueeze(0).permute([0, 3, 1, 2])
    vv.images(all_imgs.data.numpy(), nrow=int(sqrt(len(imgs))))


def forward_and_view(width, height):
    scene = create_body_scene(width, height)
    imgs = render(scene, width, height)
    view_images(imgs)


#
width, height = 512, 512
# arm_enum = enumerate_body_types(["Arm left", "Arm right"])
arm_enum = enumerate_body_types(["Eyes"])

all_imgs = []

# arm_enum = enumerate_body_types(["Eyebrows"])
for body_def in arm_enum:
    print("Creating body")
    body = create_body_scene(width, height, body_def)
    print("Rendering body")
    img = render(body, width, height)
    print("Body render {}".format(body_def))
    all_imgs.append(img)
    # view_images(imgs)

bb()
print("finished test")
#
# forward_and_view(width, height)


