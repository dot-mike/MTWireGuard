# Reverse Proxy Configuration for MTWireGuard

This document explains how to configure MTWireGuard to work behind reverse proxies like Caddy, nginx, or Traefik with TLS termination.

## Application Configuration

### Environment Variables

The application supports the following configuration approaches:

1. **Configuration file approach** (Recommended):
   - Set `ASPNETCORE_ENVIRONMENT=Production` to disable HTTPS redirection
   - Use `appsettings.Production.json` for production settings

2. **Configuration override**:
   - Set `UseHttpsRedirection=false` in appsettings or environment variables

### Key Configuration Changes Made

1. **Forwarded Headers**: The application now properly handles `X-Forwarded-For`, `X-Forwarded-Proto`, and `X-Forwarded-Host` headers.
2. **HTTPS Redirection**: Can be disabled for reverse proxy scenarios.
3. **Flexible Binding**: Listens on `0.0.0.0:8080` for container environments.

## Reverse Proxy Examples

### 1. Caddy Configuration

Create a `Caddyfile`:

```caddy
mtwireguard.yourdomain.com {
    reverse_proxy localhost:8080 {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
        header_up X-Forwarded-For {remote}
    }
}
```

Or with Docker Compose labels:

```yaml
version: '3.8'
services:
  mtwireguard:
    image: your-mtwireguard-image
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MT_IP=192.168.1.1
      - MT_USER=admin
      - MT_PASS=password
      - MT_PUBLIC_IP=your.public.ip
    labels:
      - "caddy=mtwireguard.yourdomain.com"
      - "caddy.reverse_proxy={{upstreams 8080}}"
    networks:
      - caddy
    volumes:
      - mtwireguard_data:/data

  caddy:
    image: caddy:latest
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./Caddyfile:/etc/caddy/Caddyfile
      - caddy_data:/data
      - caddy_config:/config
    networks:
      - caddy

networks:
  caddy:
    external: true

volumes:
  mtwireguard_data:
  caddy_data:
  caddy_config:
```

### 2. Nginx Configuration

Create an nginx configuration file:

```nginx
server {
    listen 80;
    server_name mtwireguard.yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name mtwireguard.yourdomain.com;

    ssl_certificate /path/to/certificate.crt;
    ssl_certificate_key /path/to/private.key;
    
    # Modern SSL configuration
    ssl_protocols TLSv1.2 TLSv1.3;
    ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384;
    ssl_prefer_server_ciphers off;

    location / {
        proxy_pass http://localhost:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        
        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }
}
```

Docker Compose with nginx:

```yaml
version: '3.8'
services:
  mtwireguard:
    image: your-mtwireguard-image
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MT_IP=192.168.1.1
      - MT_USER=admin
      - MT_PASS=password
      - MT_PUBLIC_IP=your.public.ip
    networks:
      - app-network
    volumes:
      - mtwireguard_data:/data

  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/ssl/certs
    networks:
      - app-network
    depends_on:
      - mtwireguard

networks:
  app-network:

volumes:
  mtwireguard_data:
```

### 3. Traefik Configuration

Docker Compose with Traefik:

```yaml
version: '3.8'
services:
  traefik:
    image: traefik:v3.0
    command:
      - "--api.dashboard=true"
      - "--providers.docker=true"
      - "--providers.docker.exposedbydefault=false"
      - "--entrypoints.web.address=:80"
      - "--entrypoints.websecure.address=:443"
      - "--certificatesresolvers.myresolver.acme.tlschallenge=true"
      - "--certificatesresolvers.myresolver.acme.email=your@email.com"
      - "--certificatesresolvers.myresolver.acme.storage=/letsencrypt/acme.json"
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - "/var/run/docker.sock:/var/run/docker.sock:ro"
      - "letsencrypt:/letsencrypt"
    networks:
      - traefik

  mtwireguard:
    image: your-mtwireguard-image
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - MT_IP=192.168.1.1
      - MT_USER=admin
      - MT_PASS=password
      - MT_PUBLIC_IP=your.public.ip
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.mtwireguard.rule=Host(`mtwireguard.yourdomain.com`)"
      - "traefik.http.routers.mtwireguard.entrypoints=websecure"
      - "traefik.http.routers.mtwireguard.tls.certresolver=myresolver"
      - "traefik.http.services.mtwireguard.loadbalancer.server.port=8080"
      - "traefik.http.routers.mtwireguard-http.rule=Host(`mtwireguard.yourdomain.com`)"
      - "traefik.http.routers.mtwireguard-http.entrypoints=web"
      - "traefik.http.routers.mtwireguard-http.middlewares=redirect-to-https"
      - "traefik.http.middlewares.redirect-to-https.redirectscheme.scheme=https"
    networks:
      - traefik
    volumes:
      - mtwireguard_data:/data

networks:
  traefik:
    external: true

volumes:
  mtwireguard_data:
  letsencrypt:
```

## Security Considerations

1. **Trusted Proxies**: The application is configured to trust any proxy. In production, consider restricting this to known proxy IPs.

2. **Headers**: Ensure your reverse proxy sets the correct forwarded headers:
   - `X-Forwarded-For`: Client IP address
   - `X-Forwarded-Proto`: Original protocol (http/https)
   - `X-Forwarded-Host`: Original host header

3. **WebSocket Support**: The application uses WebSockets for real-time features. Make sure your reverse proxy supports WebSocket upgrades.

## Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `ASPNETCORE_ENVIRONMENT` | ASP.NET Core environment | `Production` | No |
| `UseHttpsRedirection` | Enable/disable HTTPS redirection | `true` | No |
| `MT_IP` | Mikrotik router IP address | - | Yes |
| `MT_USER` | Mikrotik username | - | Yes |
| `MT_PASS` | Mikrotik password | - | Yes |
| `MT_PUBLIC_IP` | Public IP address | - | Yes |
| `DATA_PATH` | Data directory path | `/data` | No |

## Testing the Configuration

1. Check that the application starts without HTTPS redirection:
   ```bash
   curl -I http://your-app-url/
   ```

2. Verify that the reverse proxy correctly forwards headers:
   ```bash
   curl -H "X-Forwarded-Proto: https" -I http://your-app-url/
   ```

3. Test WebSocket connectivity (if using the real-time features).

## Troubleshooting

- **Infinite redirects**: Usually caused by the app thinking it should redirect to HTTPS while behind a TLS-terminating proxy. Set `UseHttpsRedirection=false`.
- **Authentication issues**: Make sure the `X-Forwarded-Proto` header is set correctly.
- **WebSocket failures**: Ensure your reverse proxy supports WebSocket upgrades.
