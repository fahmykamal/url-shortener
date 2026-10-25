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

## Continuous deployment

CI builds on every push (`.github/workflows/ci.yml`). The VM runs `deploy.sh` from a systemd timer every minute: it fetches `origin/main`, rebuilds, and health-checks `localhost:5076`.

Install the timer once on the VM:

```bash
sudo usermod -aG docker "$USER"   # then reconnect
sudo cp deploy/url-shortener-deploy.service deploy/url-shortener-deploy.timer /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now url-shortener-deploy.timer
```

Edit the unit files first if your VM user or repo path differs from `fahmykamal96` / `/home/fahmykamal96/url-shortener`.

Check deployments:

```bash
systemctl list-timers url-shortener-deploy.timer
journalctl -u url-shortener-deploy.service -n 50 --no-pager
```

Manual update is still possible:

```bash
git pull
docker compose up -d --build
```

Redis is not published to the host; its data persists in the `redis-data` volume. Stop with `docker compose down` (add `-v` to also delete the Redis data).
