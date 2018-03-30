import torch

import pyro
import pyro.distributions as dist


def render(struct):
    pass


class RendererWrapper(dist.Normal):
    def __init__(self, struct, scale):
        self.struct = struct
        self._scale = scale

    def sample(self):
        return dist.Normal(render(self.struct), self._scale).sample()

    def log_prob(self):
        return dist.Normal(render(self.struct), self._scale).log_prob()


def model(N):
    # most basic model: generate single path of fixed number of control points
    # intermediate model: generate DAG (unbalanced tree?) of control points
    # after adding each point and adding edges, draw and send to Unity
    # Render with a fixed camera pose and return
    # Observe target image with ABC likelihood given by rendered sample(s)

    # generate root
    points = [torch.zeros(3)]
    points.append(pyro.sample("root", dist.Normal(torch.zeros(3), torch.ones(3))) - points[0])

    # generate latents
    images = []
    for t in range(N):
        points.append(pyro.sample("coord{}".format(t),
                                  dist.Normal(points[-1], torch.ones(3))) - points[-2])

        # observe
        images.append(pyro.sample("render{}".format(t),
                                  RendererWrapper(points, 1.0)))

    return points, images
