# Dev conatiner

- [General options](https://code.visualstudio.com/docs/devcontainers/containers)


# Some useful commands

Delete all elasticsearc indexes

```bash
curl -s 'http://elasticsearch:9200/_cat/indices?h=index' | xargs -r -n1 -I{} curl -X DELETE "http://elasticsearch:9200/{}?expand_wildcards=all&pretty"
```