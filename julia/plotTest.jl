using Plots

pyplot() # set pyplot and matplotlib as backend

x = range(0, stop=2*pi, length=1000)
y = sin.(x)  #use the '.' as in matlab

plt = plot(x,y)
savefig(plt, "test.png")
