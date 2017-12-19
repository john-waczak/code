import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from matplotlib import animation


G = 1e4
dt = 1e-2
planets = []
numIterations = 1000
scale = 100


class State:
    """Current state"""
    def __init__(self, x, y, z, vx, vy, vz):
        self.x = x
        self.y = y
        self.z = z
        self.vx = vx
        self.vy = vy
        self.vz = vz

    def __repr__(self):
        return "x:{0} y:{1} z:{2} vx:{3} vy:{4} vz:{5}".format(
            self.x, self.y, self.z, self.vx, self.vy, self.vz)


class Planet:
    """Class representing an arbitrary planet"""
    def __init__(self, radius, mass, xi, yi, zi, vix, viy, viz):
        self.r = radius
        self.m = mass
        self.state = State(
            xi,
            yi,
            zi,
            vix,
            viy,
            viz)

    def __repr__(self):
        return repr(self.state)

    def get2BodyForce(self, body2):
        del_x = body2.state.x - self.state.x
        del_y = body2.state.y - self.state.y
        del_z = body2.state.z - self.state.z
        norm = np.sqrt(del_x**2 + del_y**2 + del_z**2)
        dx = del_x/norm
        dy = del_y/norm
        dz = del_z/norm
        F = G*body2.m*self.m/norm**2
        Fx = F*dx
        Fy = F*dy
        Fz = F*dz
        return Fx, Fy, Fz

    def getNetForce(self):
        Fxs = []
        Fys = []
        Fzs = []
        for body in planets:
            if body == self:
                pass
            else:
                Fx, Fy, Fz = self.get2BodyForce(body)
                Fxs.append(Fx)
                Fys.append(Fy)
                Fzs.append(Fz)
        Net_Fx = sum(Fxs)
        Net_Fy = sum(Fys)
        Net_Fz = sum(Fzs)
        return Net_Fx, Net_Fy, Net_Fz

    def updateState(self, Fx, Fy, Fz):
        ax = Fx/self.m
        ay = Fy/self.m
        az = Fz/self.m

        self.state.x = self.state.x + self.state.vx*dt + 0.5*ax*dt**2
        self.state.y = self.state.y + self.state.vy*dt + 0.5*ay*dt**2
        self.state.z = self.state.z + self.state.vz*dt + 0.5*az*dt**2

        self.state.vx = self.state.vx + ax*dt
        self.state.vy = self.state.vy + ay*dt
        self.state.vz = self.state.vz + az*dt


if __name__ == "__main__":
    planets.append(Planet(5, 100, 0, 0.0001, 0,  0, 0, 0))
    planets.append(Planet(1, 1, 0, 20, 0, 200, 0, 0))
    planets.append(Planet(1, 1, 0, 0, 30, 200, 0, 0))
    planets.append(Planet(1, 1, 0, -20, 0, -200, 0, 0))
    planets.append(Planet(1, 10, 20, -50, 40, 100, 50, -30))

    numPlanets = len(planets)

    X = np.zeros((numPlanets, numIterations))
    Y = np.zeros((numPlanets, numIterations))
    Z = np.zeros((numPlanets, numIterations))
    
    Fx = np.zeros(numPlanets)
    Fy = np.zeros(numPlanets)
    Fz = np.zeros(numPlanets)

    for i in range(numIterations):
        # need two loops to update positions simultaneously
        for j in range(numPlanets):
            Fx[j], Fy[j], Fz[j] = planets[j].getNetForce()
        for j in range(numPlanets):
            planets[j].updateState(Fx[j], Fy[j], Fz[j])
            X[j, i] = planets[j].state.x
            Y[j, i] = planets[j].state.y
            Z[j, i] = planets[j].state.z

    fig = plt.figure()
    ax = fig.gca(projection='3d')
    ax.axis('off')
    lines = []
    pts = []

    for i in range(numPlanets):
        line, = ax.plot([], [], [], lw=2, color='k', alpha=0.25)
        pt, = ax.plot([], [], [], 'ko', ms=planets[i].r)
        lines.append(line)
        pts.append(pt)

    ax.set_xlim((-scale, scale))
    ax.set_ylim((-scale, scale))
    ax.set_zlim((-scale, scale))

    def init():
        for j in range(numPlanets):
            line.set_data([], [])
            line.set_3d_properties([])
            pts.set_data([], [])
            pts.set_3d_properties([])
            
    def animate(i, X, Y, Z):
        for j in range(numPlanets):
            lines[j].set_data(X[j, :i+1], Y[j, :i+1])
            lines[j].set_3d_properties(Z[j, :i+1])
            pts[j].set_data(X[j, i], Y[j, i])
            pts[j].set_3d_properties(Z[j, i])
            
    anim = animation.FuncAnimation(fig, animate, frames=numIterations-1,
                                   interval=100, fargs=(X, Y, Z))

    plt.show()
