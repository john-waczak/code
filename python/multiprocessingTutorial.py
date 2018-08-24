import multiprocessing


def job(num):
    return num*2


def spawn(num):
    print("Spawned! ", num) 


if __name__ == '__main__':
    p = multiprocessing.Pool(processes=20)
    data = p.map(job, range(20))
    p.close()
    print(data)


    for i in range(50):
        p = multiprocessing.Process(target=spawn, args=(i,))
        p.start()
