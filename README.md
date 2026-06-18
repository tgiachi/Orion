# OrionIrcd

## Overview

Short description goes here.

## Build

```bash
dotnet build OrionIrcd.slnx
```

## Test

```bash
dotnet test OrionIrcd.slnx
```

## Docker

```bash
docker build -t orionircd-server .
docker run --rm -it -p 6666:6666 -p 6667:6667 -p 6668:6668 -v orionircd-data:/data orionircd-server
```

## License

MIT - see [LICENSE](LICENSE).
