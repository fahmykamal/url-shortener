# url-shortener

ASP.NET Core 9 URL shortener backed by Redis.

## Local

Requires the .NET 9 SDK and Docker. The SDK is only needed for local development; on the VM you only need Docker and Docker Compose, since the SDK and ASP.NET runtime are pulled as container images.

```bash
docker run -d --rm -p 6379:6379 --name url-redis redis:7-alpine
dotnet run
```

## Docker

```bash
docker compose up -d --build
```

Open `http://localhost:5076`.

## VM deployment

```bash
git clone https://github.com/fahmykamal/url-shortener.git
cd url-shortener
docker compose up -d --build
```

Allow port `5076` (or set `API_PORT` before starting) in the VM firewall, then open `http://<vm-ip>:5076`.

Update a running deployment:

```bash
git pull
docker compose up -d --build
```

Redis is not published to the host; its data persists in the `redis-data` volume. Stop with `docker compose down` (add `-v` to also delete the Redis data).
