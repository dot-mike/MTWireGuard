# SSL Configuration for Mikrotik API

## Overview

MTWireGuard now supports configurable SSL/TLS connections to the Mikrotik API. By default, the application attempts to connect using HTTP (port 80) for backward compatibility, but you can enable SSL/HTTPS connections using the `MT_API_SSL` environment variable.

## Configuration

### Environment Variable

Add the following environment variable to enable SSL:

```bash
export MT_API_SSL=true
```

### Default Behavior

- **Default**: `MT_API_SSL=false` (HTTP on port 80)
- **When enabled**: `MT_API_SSL=true` (HTTPS on port 443)

### Port Configuration

The application automatically selects the appropriate port based on the SSL setting:

- **HTTP**: Port 80 (when `MT_API_SSL=false`)
- **HTTPS**: Port 443 (when `MT_API_SSL=true`)

If your Mikrotik router is configured to use custom ports, you can specify them directly in the `MT_IP` variable:

```bash
# Custom HTTPS port
export MT_IP=192.168.1.1:8443
export MT_API_SSL=true

# Custom HTTP port  
export MT_IP=192.168.1.1:8080
export MT_API_SSL=false
```

## Mikrotik Router Configuration

### Enabling HTTPS on Mikrotik

To use SSL connections, ensure your Mikrotik router has HTTPS enabled:

1. **Enable HTTPS service**:
   ```
   /ip service enable www-ssl
   /ip service set www-ssl port=443
   ```

2. **Generate or upload SSL certificate** (optional but recommended for production):
   ```
   /certificate add name=server-template common-name=your-router-name
   /certificate sign server-template
   ```

3. **Configure HTTPS to use the certificate**:
   ```
   /ip service set www-ssl certificate=server-template
   ```

### Disabling HTTP (Optional)

For security, you may want to disable HTTP once HTTPS is working:

```
/ip service disable www
```

## Troubleshooting

### SSL Connection Errors

The application now provides detailed error messages for SSL connection issues. Common problems and solutions:

1. **"SSL connection could not be established"**
   - Verify `MT_API_SSL=true` is set
   - Ensure Mikrotik router has HTTPS enabled
   - Check that port 443 is accessible

2. **"Connection timed out"**
   - Verify the router IP address is correct
   - Check network connectivity
   - Ensure firewall rules allow the connection

3. **"Certificate validation failed"**
   - The application automatically accepts self-signed certificates
   - This should not be an issue in most cases

### Testing Connection

You can test the connection manually using curl:

```bash
# Test HTTP connection
curl -k -u username:password http://192.168.1.1/rest/

# Test HTTPS connection  
curl -k -u username:password https://192.168.1.1/rest/
```

## Security Considerations

- **Production environments**: Always use HTTPS (`MT_API_SSL=true`)
- **Self-signed certificates**: The application accepts all certificates for simplicity, but consider using proper certificates in production
- **Credentials**: Ensure `MT_USER` and `MT_PASS` are properly secured
- **Network**: Use firewall rules to restrict API access to trusted sources

## Example Configuration

### Complete environment setup for HTTPS:

```bash
export MT_IP=192.168.1.1
export MT_USER=api-user
export MT_PASS=secure-password
export MT_API_SSL=true
export MT_PUBLIC_IP=your-public-domain.com
export ASPNETCORE_ENVIRONMENT=Production
```

### Complete environment setup for HTTP (development):

```bash
export MT_IP=192.168.1.1
export MT_USER=api-user  
export MT_PASS=secure-password
export MT_API_SSL=false
export MT_PUBLIC_IP=localhost
export ASPNETCORE_ENVIRONMENT=Development
```
