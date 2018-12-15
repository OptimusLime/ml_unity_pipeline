import torch

import pyro
import pyro.distributions as dist
import numpy as np
from unity_connect import BasicClient, screenshot_to_np
from examples.splines.render import make_gradient_weight_similarity_fct
from examples.splines.render import normalized_similarity, PtImg2D, load_target
from proto.examples_pb2 import ProtoSpline, ProtoV3
from proto.models_pb2 import ProtoScreenShot
from IPython import embed
from pdb import set_trace as bb
import visdom
from math import sqrt


vv = visdom.Visdom(env="dope")

# queue up
queue_name = "task_queue"

# connect
client = BasicClient(queue_name).connect()

tgt_img = load_target("examples/vines/g.png")
sim_fct = make_gradient_weight_similarity_fct(1.5)


# send a spline over to be rendered
def unity_render_spline(points, width, height, spline_type="Bezier"):
    # create our spline object
    ps = ProtoSpline()
    ps.type = spline_type
    ps.screen_width = width
    ps.screen_height = height

    for pt_point in points:
        # add to our control points, off we go
        ps_point = ps.control_points.add()

        ps_point.x = pt_point[0].item()
        ps_point.y = pt_point[1].item()
        ps_point.z = 0  # pt_point.data[2]

    ss = ProtoScreenShot()
    ss.width = width
    ss.height = height

    # send out to client please
    cs_response = client.sync_call(client.get_message(ps, ss))
    np_ss = screenshot_to_np(cs_response[1][1])
    return np_ss


def render(struct, width, height, spline_type):

    # numpy version of image coming back
    np_img = unity_render_spline(struct, width, height, spline_type)

    # send back a pytorch version of the image
    # wrapped with handler for the similarity fct
    return PtImg2D(pt_img=torch.from_numpy(np_img))


class RendererWrapper(dist.Distribution):
    def __init__(self, struct, width, height, spline_type):
        self.struct = struct
        self.spline_type = spline_type
        self.width = width
        self.height = height

    def sample(self):
        return render(self.struct, self.width, self.height, self.spline_type)

    def log_prob(self, x):
        return torch.log(normalized_similarity(x, tgt_img, sim_fct))


def model(N, width, height, spline_type):
    # most basic model: generate single path of fixed number of control points
    # intermediate model: generate DAG (unbalanced tree?) of control points
    # after adding each point and adding edges, draw and send to Unity
    # Render with a fixed camera pose and return
    # Observe target image with ABC likelihood given by rendered sample(s)

    # generate root
    points = [torch.zeros(3)]

    # normal around zero
    points.append(pyro.sample("root",
                              dist.Normal(torch.zeros(3),
                                          torch.ones(3))) - points[0])

    # generate latents
    images = []
    for t in range(N):
        points.append(pyro.sample("coord{}".format(t),
                                  dist.Normal(points[-1], torch.ones(3))))

        # observe
        images.append(pyro.sample("render{}".format(t),
                                  RendererWrapper(points,
                                                  width,
                                                  height,
                                                  spline_type)))

    return points, images


def view_images(imgs):
    all_imgs = torch.cat(list(map(
            lambda x: x.pt_img.view([-1] + list(x.pt_img.size())).permute([0, 3, 1, 2]),
            imgs)))

    vv.images(all_imgs.numpy(), nrow=int(sqrt(len(imgs))))


def forward_and_view(point_count, width, height, spline_type):
    pts, imgs = model(point_count, width, height, spline_type)
    view_images(imgs)

spline_type = "Hermite"
points, width, height = 16, 100, 100
forward_and_view(points, width, height, spline_type)
# embed()


