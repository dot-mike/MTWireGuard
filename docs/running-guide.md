# Running MTWireGuard - Complete Setup Guide

MTWireGuard is a web application for managing Mikrotik WireGuard interfaces. This guide covers all the different ways to run the application based on the latest updates from the dev branch.

## 🚀 Quick Start

### Prerequisites

- Docker and Docker Compose
- A Mikrotik router with WireGuard support
- .NET 8.0 (if running without Docker)

### Environment Variables

Before running MTWireGuard, you need to configure these essential environment variables:

| Variable | Description | Example | Required |
|----------|-------------|---------|----------|
| `MT_IP` | Mikrotik router IP address | `192.168.1.1` | ✅ |
| `MT_USER` | Mikrotik username | `admin` | ✅ |
| `MT_PASS` | Mikrotik password | `your_password` | ✅ |
| `MT_PUBLIC_IP` | Your public IP/domain | `your.public.ip` | ✅ |
| `MT_API_SSL` | Enable SSL for Mikrotik API | `true` or `false` | ❌ |
| `ASPNETCORE_ENVIRONMENT` | Application environment | `Production` | ❌ |
| `DATA_PATH` | Data directory path | `/data` | ❌ |
| `TZ` | Timezone | `Europe/Berlin` | ❌ |

## 🐳 Method 1: Docker Compose (Recommended)

### Option A: Standalone Deployment

Create a `docker-compose.yml` file:

```yaml
version: '3.8'

services:
  mtwireguard:
    image: techgarageir/mtwireguard:latest
    environment:
      - MT_IP=192.168.1.1
      - MT_USER=admin
      - MT_PASS=your_password
      - MT_PUBLIC_IP=your.public.ip
      - MT_API_SSL=false
      - ASPNETCORE_ENVIRONMENT=Production
      - DATA_PATH=/data
      - TZ=Europe/Berlin
    
    ports:
      - "8080:8080"
    
    volumes:
      - mtwireguard_data:/data
    
    restart: unless-stopped
    
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  mtwireguard_data:
```

Run the application:

```bash
docker-compose up -d
```

Access the application at `http://localhost:8080`

### Option B: Reverse Proxy Deployment (Production Recommended)

For production deployments with TLS/SSL, use the reverse proxy configuration:

```bash
# Use the provided reverse proxy template
cp docker-compose.reverse-proxy.yml docker-compose.yml

# Edit the configuration for your environment
nano docker-compose.yml

# Start the application
docker-compose up -d
```

## 🌐 Method 2: Behind Reverse Proxy with TLS

MTWireGuard supports running behind reverse proxies like Caddy, nginx, or Traefik. This is the **recommended approach for production**.

### Key Configuration for Reverse Proxy

- Set `ASPNETCORE_ENVIRONMENT=Production`
- Set `UseHttpsRedirection=false` (TLS handled by proxy)
- Application listens on `0.0.0.0:8080` internally

### Caddy Example

Create a `Caddyfile`:

```caddy
mtwireguard.yourdomain.com {
    reverse_proxy mtwireguard:8080 {
        header_up X-Forwarded-Proto {scheme}
        header_up X-Forwarded-Host {host}
        header_up X-Forwarded-For {remote}
    }
}
```

### nginx Example

```nginx
server {
    listen 443 ssl http2;
    server_name mtwireguard.yourdomain.com;

    location / {
        proxy_pass http://mtwireguard:8080;
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Forwarded-Host $host;
        
        # WebSocket support
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

### Traefik Example

Add these labels to your service:

```yaml
labels:
  - "traefik.enable=true"
  - "traefik.http.routers.mtwireguard.rule=Host(`mtwireguard.yourdomain.com`)"
  - "traefik.http.routers.mtwireguard.entrypoints=websecure"
  - "traefik.http.routers.mtwireguard.tls.certresolver=myresolver"
  - "traefik.http.services.mtwireguard.loadbalancer.server.port=8080"
```

For complete reverse proxy setup, see: [docs/reverse-proxy-setup.md](docs/reverse-proxy-setup.md)

## 🔧 Method 3: Local Development Build

For development or building from source:

### Using Docker Development Build

```bash
# Clone the repository
git clone https://github.com/dot-mike/MTWireGuard.git
cd MTWireGuard

# Build and run using development Dockerfile
docker build -f Dockerfile.dev -t mtwireguard:dev .

# Run the container
docker run -d \
  --name mtwireguard \
  -p 8080:8080 \
  -e MT_IP=192.168.1.1 \
  -e MT_USER=admin \
  -e MT_PASS=your_password \
  -e MT_PUBLIC_IP=your.public.ip \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -v mtwireguard_data:/data \
  mtwireguard:dev
```

### Using .NET SDK

```bash
# Clone the repository
git clone https://github.com/dot-mike/MTWireGuard.git
cd MTWireGuard

# Restore dependencies
dotnet restore

# Build the application
dotnet build

# Set environment variables
export MT_IP=192.168.1.1
export MT_USER=admin
export MT_PASS=your_password
export MT_PUBLIC_IP=your.public.ip
export ASPNETCORE_ENVIRONMENT=Development

# Run the application
dotnet run --project UI/MTWireGuard.csproj
```

## 🔧 Method 4: Pre-built Binary

If you have the pre-built binary (from the `publish/` directory):

```bash
# Make sure the binary is executable
chmod +x MTWireGuard

# Set environment variables
export MT_IP=192.168.1.1
export MT_USER=admin
export MT_PASS=your_password
export MT_PUBLIC_IP=your.public.ip
export ASPNETCORE_ENVIRONMENT=Production
export DATA_PATH=/data

# Run the application
./MTWireGuard
```

## 🔐 Mikrotik Configuration

### Basic Mikrotik Setup

1. **Enable API service**:

   ```bash
   /ip service enable api
   /ip service set api port=8728
   ```

2. **For HTTPS API (recommended)**:

   ```bash
   /ip service enable api-ssl
   /ip service set api-ssl port=8729
   ```

   Set `MT_API_SSL=true` in your environment variables.

3. **Create dedicated API user** (recommended):

   ```bash
   /user add name=api-user password=secure-password group=full
   ```

### WireGuard Interface Setup

Create a WireGuard interface on your Mikrotik:

```bash
/interface wireguard add name=wireguard1
/interface wireguard set wireguard1 listen-port=13231
/ip address add address=10.0.0.1/24 interface=wireguard1
```

## 📋 Environment File (.env)

Create a `.env` file for easier configuration:

```bash
# Mikrotik Configuration
MT_IP=192.168.1.1
MT_USER=admin
MT_PASS=your_password
MT_PUBLIC_IP=your.public.ip
MT_API_SSL=false

# Application Configuration
ASPNETCORE_ENVIRONMENT=Production
DATA_PATH=/data
TZ=Europe/Berlin

# Optional: Disable HTTPS redirection for reverse proxy
UseHttpsRedirection=false
```

Use with Docker Compose:

```yaml
version: '3.8'
services:
  mtwireguard:
    image: techgarageir/mtwireguard:latest
    env_file: .env
    ports:
      - "8080:8080"
    volumes:
      - mtwireguard_data:/data
    restart: unless-stopped

volumes:
  mtwireguard_data:
```

## 🔍 Health Checks and Monitoring

MTWireGuard provides health check endpoints:

- **Health check**: `GET /health` - Basic application health
- **Readiness check**: `GET /ready` - Includes Mikrotik API connectivity

Test the endpoints:

```bash
# Basic health check
curl http://localhost:8080/health

# Readiness check (tests Mikrotik connection)
curl http://localhost:8080/ready
```

Example response:

```json
{
  "status": "healthy",
  "timestamp": "2025-07-22T10:30:00Z",
  "version": "1.0.0"
}
```

## 🚀 Production Deployment Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Use `MT_API_SSL=true` for secure Mikrotik connection
- [ ] Deploy behind reverse proxy with TLS
- [ ] Set `UseHttpsRedirection=false` when using reverse proxy
- [ ] Configure proper backup for `/data` volume
- [ ] Set up monitoring using health check endpoints
- [ ] Use strong passwords for Mikrotik API user
- [ ] Restrict Mikrotik API access to trusted IPs
- [ ] Configure proper firewall rules

## 🐛 Troubleshooting

### Common Issues

1. **Application fails to start**:
   - Check environment variables are set correctly
   - Verify Mikrotik API connectivity
   - Check logs: `docker logs mtwireguard`

2. **Cannot connect to Mikrotik**:
   - Verify `MT_IP`, `MT_USER`, `MT_PASS` are correct
   - Check if Mikrotik API service is enabled
   - Test API connection manually:

     ```bash
     curl -u admin:password http://192.168.1.1:8728/rest/system/identity
     ```

3. **SSL/TLS connection issues**:
   - For HTTP: Set `MT_API_SSL=false`
   - For HTTPS: Set `MT_API_SSL=true` and ensure SSL is enabled on Mikrotik

4. **Reverse proxy issues**:
   - Set `UseHttpsRedirection=false`
   - Ensure forwarded headers are configured
   - Check proxy configuration passes correct headers

### Logs

View application logs:

```bash
# Docker Compose
docker-compose logs -f mtwireguard

# Docker
docker logs -f mtwireguard

# Direct run
# Logs are written to console and configured Serilog sinks
```

### Debug Mode

For development/debugging, set:

```bash
export ASPNETCORE_ENVIRONMENT=Development
export Logging__LogLevel__Default=Debug
```

## 📁 Data Persistence

The application stores data in SQLite database and configuration files. Make sure to:

- Mount the `/data` volume for persistence
- Backup the data volume regularly
- The database file is located at `${DATA_PATH}/MTWireGuard.db`

## 🔗 Additional Resources

- [SSL Configuration Guide](docs/ssl-configuration.md)
- [Reverse Proxy Setup Guide](docs/reverse-proxy-setup.md)
- [Project Wiki](https://github.com/techgarage-ir/MTWireGuard/wiki)
- [Report Issues](https://github.com/dot-mike/MTWireGuard/issues)

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---

**Note**: This project is for educational purposes. Any illegal usage is solely the responsibility of the user.
