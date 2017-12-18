import numpy as np
import matplotlib.pyplot as plt
from matplotlib import animation


G = 1e4
dt = 1e-3
planets = []


class State:
    """Current state"""
    def __init__(self, x, y, vx, vy):
        self.x = x
        self.y = y
        self.vx = vx
        self.vy = vy

    def __repr__(self):
        return "x:{0} y:{1} vx:{2} vy:{3}".format(
            self.x, self.y, self.vx, self.vy)


class Planet:
    """Class representing an arbitrary planet"""
    def __init__(self, radius, mass, xi, yi, vix, viy):
        self.r = radius
        self.m = mass
        self.state = State(
            xi,
            yi,
            vix,
            viy)

    def __repr__(self):
        return repr(self.state)

    def get2BodyForce(self, body2):
        del_x = body2.state.x - self.state.x
        del_y = body2.state.y - self.state.y
        norm = np.sqrt(del_x**2+del_y**2)
        dx = del_x/norm
        dy = del_y/norm
        F = G*body2.m*self.m/norm**2
        Fx = F*dx
        Fy = F*dy
        return Fx, Fy

    def getNetForce(self):
        Fxs = []
        Fys = []
        for body in planets:
            if body == self:
                pass
            else:
                Fx, Fy = self.get2BodyForce(body)
                Fxs.append(Fx)
                Fys.append(Fy)
        Net_Fx = sum(Fxs)
        Net_Fy = sum(Fys)
        return Net_Fx, Net_Fy

    def updateState(self, Fx, Fy):
        ax = Fx/self.m
        ay = Fy/self.m

        self.state.x = self.state.x + self.state.vx*dt + 0.5*ax*dt**2
        self.state.y = self.state.y + self.state.vy*dt + 0.5*ay*dt**2

        self.state.vx = self.state.vx + ax*dt
        self.state.vy = self.state.vy + ay*dt


if __name__ == "__main__":
    # planets.append(Planet(1, 1, 0, 0, 0, 10))
    # planets.append(Planet(1, 1, 10, 10, 0, -10))
    # planets.append(Planet(1, 1, 0, 10, 10, 0))
    # planets.append(Planet(1, 1, 10, 0, -10, 0))
    planets.append(Planet(1, 100, 0, 0.0001, 0, 0))
    planets.append(Planet(1, 1, 0, 20, 200, 0))
    planets.append(Planet(1, 1, 0, -20, -200, 0))
    planets.append(Planet(1, 1, 20, 0, 0, -200))
    planets.append(Planet(1, 1, -20, 0, 0, 200))
    planets.append(Planet(1, 0.5, 0, 30, 100, 0))

    numPlanets = len(planets)
    numIterations = 1000
    X = np.zeros((numPlanets, numIterations))
    Y = np.zeros((numPlanets, numIterations))
    Fx = np.zeros(numPlanets)
    Fy = np.zeros(numPlanets)
    
    for i in range(numIterations):
        #need two loops to update positions simultaneously
        for j in range(numPlanets):
            Fx[j], Fy[j] = planets[j].getNetForce()
        for j in range(numPlanets):
            planets[j].updateState(Fx[j], Fy[j])
            X[j, i] = planets[j].state.x
            Y[j, i] = planets[j].state.y

    scale = 50
    fig = plt.figure()
    ax = plt.axes(xlim=(-scale, scale), ylim=(-scale, scale))
    

    lines = []
    pts = []
    for i in range(numPlanets):
        line, = ax.plot([], [], lw=2, color='k', alpha=0.35)
        pt, = ax.plot([], [], 'ko', ms=3)
        lines.append(line)
        pts.append(pt)
        
    def animate(i, X, Y):
        for j in range(numPlanets):
            lines[j].set_data(X[j, :i+1], Y[j, :i+1])
            pts[j].set_data(X[j, i], Y[j, i])

    anim = animation.FuncAnimation(fig, animate, frames=numIterations-1,
                                   interval=2, fargs=(X, Y))
    
    plt.show()
