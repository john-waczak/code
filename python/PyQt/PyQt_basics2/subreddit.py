import urllib2
import json
import time


def get_top_post(subreddit):
    url = "https://www.reddit.com/r/{}.json?limit=1".format(subreddit)
    headers = {'User-Agent': 'nikolak@outlook.com tutorial code'}
    request = urllib2.Request(url, headers=headers)
    response = urllib2.urlopen(request)
    data = json.load(response)
    top_post = data['data']['children'][0]['data']
    return "'{title}' by {author} in {subreddit}".format(**top_post)


def get_top_from_subreddits(subreddits):
    for subreddit in subreddits:
        yield get_top_post(subreddit)
        time.sleep(2)  # reddit wants one request per two seconds


if __name__ == '__main__':
    for post in get_top_from_subreddits(['python', 'linux', 'learnpython']):
        print post
