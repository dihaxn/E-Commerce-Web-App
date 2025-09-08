# 🚀 **E-COMMERCE APPLICATION DEPLOYMENT GUIDE**

## 📋 **TABLE OF CONTENTS**

1. [Prerequisites](#prerequisites)
2. [Local Production Testing](#local-production-testing)
3. [Cloud Deployment (Azure)](#cloud-deployment-azure)
4. [Cloud Deployment (AWS)](#cloud-deployment-aws)
5. [Docker Deployment](#docker-deployment)
6. [On-Premises Server](#on-premises-server)
7. [Post-Deployment](#post-deployment)
8. [Troubleshooting](#troubleshooting)

---

## 🔧 **PREREQUISITES**

### **Required Software**
- .NET 8.0 SDK
- SQL Server (or SQL Server Express)
- PowerShell 5.1+ (Windows)
- Git

### **Required Accounts**
- Brevo (email service) API key
- PayPal developer account
- SSL certificate (for production)

---

## 🖥️ **LOCAL PRODUCTION TESTING**

### **Step 1: Configure Environment**
```bash
# Copy environment template
copy deploy\production.env.template deploy\production.env

# Edit production.env with your values
notepad deploy\production.env
```

### **Step 2: Run Deployment Script**
```powershell
# Run the simplified deployment script
powershell -ExecutionPolicy Bypass -File deploy\deploy-simple.ps1 -Environment Production

# Or with parameters
powershell -ExecutionPolicy Bypass -File deploy\deploy-simple.ps1 -Environment Production -ConnectionString "your-connection-string"
```

### **Step 3: Start Application**
```bash
# Navigate to publish directory
cd publish\Production

# Start the application
start-Production.bat
```

### **Step 4: Verify Deployment**
- Open: `https://localhost:5001`
- Health Check: `https://localhost:5001/api/health`
- Database: Verify connection in logs

---

## ☁️ **CLOUD DEPLOYMENT (AZURE)**

### **Option A: Azure App Service**

#### **Step 1: Create Azure Resources**
```bash
# Login to Azure
az login

# Create resource group
az group create --name ECommerce-RG --location EastUS

# Create App Service plan
az appservice plan create --name ECommerce-Plan --resource-group ECommerce-RG --sku B1 --is-linux

# Create web app
az webapp create --name your-ecommerce-app --resource-group ECommerce-RG --plan ECommerce-Plan --runtime "DOTNETCORE:8.0"
```

#### **Step 2: Configure Environment Variables**
```bash
# Set environment variables
az webapp config appsettings set --name your-ecommerce-app --resource-group ECommerce-RG --settings \
  ASPNETCORE_ENVIRONMENT=Production \
  ConnectionStrings__DefaultConnection="your-connection-string" \
  BrevoSettings__ApiKey="your-brevo-key" \
  PayPalSettings__ClientId="your-paypal-id" \
  PayPalSettings__Secret="your-paypal-secret"
```

#### **Step 3: Deploy Application**
```bash
# Publish application
dotnet publish -c Release -o publish/azure

# Deploy to Azure
az webapp deployment source config-zip --resource-group ECommerce-RG --name your-ecommerce-app --src publish/azure.zip
```

### **Option B: Azure Container Instances**

#### **Step 1: Create Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["E-Commerce-BE.csproj", "./"]
RUN dotnet restore "E-Commerce-BE.csproj"
COPY . .
RUN dotnet build "E-Commerce-BE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "E-Commerce-BE.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "E-Commerce-BE.dll"]
```

#### **Step 2: Build and Deploy**
```bash
# Build Docker image
docker build -t ecommerce-app .

# Push to Azure Container Registry
az acr build --registry your-registry --image ecommerce-app .

# Deploy to Container Instances
az container create --resource-group ECommerce-RG --name ecommerce-container --image your-registry.azurecr.io/ecommerce-app --ports 80 443
```

---

## ☁️ **CLOUD DEPLOYMENT (AWS)**

### **Option A: AWS Elastic Beanstalk**

#### **Step 1: Create EB Application**
```bash
# Install EB CLI
pip install awsebcli

# Initialize EB application
eb init ecommerce-app --platform dotnet-core --region us-east-1

# Create environment
eb create production-env --instance-type t2.micro
```

#### **Step 2: Configure Environment**
```bash
# Set environment variables
eb setenv ASPNETCORE_ENVIRONMENT=Production
eb setenv ConnectionStrings__DefaultConnection="your-connection-string"
eb setenv BrevoSettings__ApiKey="your-brevo-key"
```

#### **Step 3: Deploy**
```bash
# Deploy application
eb deploy
```

### **Option B: AWS ECS with Fargate**

#### **Step 1: Create Task Definition**
```json
{
  "family": "ecommerce-app",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "cpu": "256",
  "memory": "512",
  "executionRoleArn": "arn:aws:iam::account:role/ecsTaskExecutionRole",
  "containerDefinitions": [
    {
      "name": "ecommerce-app",
      "image": "your-ecr-repo/ecommerce-app:latest",
      "portMappings": [
        {
          "containerPort": 80,
          "protocol": "tcp"
        }
      ],
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ]
    }
  ]
}
```

#### **Step 2: Deploy to ECS**
```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name ecommerce-cluster

# Register task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Create service
aws ecs create-service --cluster ecommerce-cluster --service-name ecommerce-service --task-definition ecommerce-app:1
```

---

## 🐳 **DOCKER DEPLOYMENT**

### **Step 1: Create Dockerfile**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["E-Commerce-BE.csproj", "./"]
RUN dotnet restore "E-Commerce-BE.csproj"
COPY . .
RUN dotnet build "E-Commerce-BE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "E-Commerce-BE.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "E-Commerce-BE.dll"]
```

### **Step 2: Build and Run**
```bash
# Build image
docker build -t ecommerce-app .

# Run container
docker run -d -p 8080:80 -p 8443:443 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e ConnectionStrings__DefaultConnection="your-connection-string" \
  --name ecommerce-container \
  ecommerce-app
```

### **Step 3: Docker Compose (Recommended)**
```yaml
# docker-compose.yml
version: '3.8'
services:
  ecommerce-app:
    build: .
    ports:
      - "8080:80"
      - "8443:443"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION_STRING}
      - BrevoSettings__ApiKey=${BREVO_API_KEY}
      - PayPalSettings__ClientId=${PAYPAL_CLIENT_ID}
      - PayPalSettings__Secret=${PAYPAL_SECRET}
    depends_on:
      - database
  
  database:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${DB_PASSWORD}
    ports:
      - "1433:1433"
```

```bash
# Run with Docker Compose
docker-compose up -d
```

---

## 🏢 **ON-PREMISES SERVER**

### **Step 1: Server Requirements**
- Windows Server 2019+ or Linux (Ubuntu 20.04+)
- .NET 8.0 Runtime
- SQL Server 2019+
- IIS (Windows) or Nginx (Linux)
- SSL Certificate

### **Step 2: Deploy Application**
```bash
# Publish application
dotnet publish -c Release -o /var/www/ecommerce-app

# Set permissions (Linux)
sudo chown -R www-data:www-data /var/www/ecommerce-app
sudo chmod -R 755 /var/www/ecommerce-app
```

### **Step 3: Configure Web Server**

#### **IIS (Windows)**
```xml
<!-- web.config -->
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <location path="." inheritInChildApplications="false">
    <system.webServer>
      <handlers>
        <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
      </handlers>
      <aspNetCore processPath="dotnet" arguments=".\E-Commerce-BE.dll" stdoutLogEnabled="false" stdoutLogFile=".\logs\stdout" hostingModel="inprocess" />
    </system.webServer>
  </location>
</configuration>
```

#### **Nginx (Linux)**
```nginx
# /etc/nginx/sites-available/ecommerce-app
server {
    listen 80;
    server_name your-domain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name your-domain.com;
    
    ssl_certificate /etc/ssl/certs/your-cert.pem;
    ssl_certificate_key /etc/ssl/private/your-key.pem;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

---

## ✅ **POST-DEPLOYMENT**

### **Health Checks**
```bash
# Verify application health
curl https://your-domain.com/api/health

# Check database connection
curl https://your-domain.com/api/health/database

# Monitor application logs
tail -f /var/log/ecommerce-app/app.log
```

### **Security Verification**
```bash
# Check security headers
curl -I https://your-domain.com

# Verify HTTPS enforcement
curl http://your-domain.com  # Should redirect to HTTPS

# Test CSRF protection
# Attempt to submit forms without anti-forgery tokens
```

### **Performance Testing**
```bash
# Load testing with Apache Bench
ab -n 1000 -c 10 https://your-domain.com/

# Database performance
# Monitor query execution times in logs
```

---

## 🔧 **TROUBLESHOOTING**

### **Common Issues**

#### **Database Connection Failed**
```bash
# Check connection string
echo $ConnectionStrings__DefaultConnection

# Test database connectivity
sqlcmd -S your-server -U your-user -P your-password -Q "SELECT 1"
```

#### **Application Won't Start**
```bash
# Check logs
dotnet E-Commerce-BE.dll

# Verify environment variables
echo $ASPNETCORE_ENVIRONMENT
echo $ConnectionStrings__DefaultConnection
```

#### **SSL Certificate Issues**
```bash
# Verify certificate
openssl x509 -in your-cert.pem -text -noout

# Check certificate chain
openssl verify -CAfile ca-bundle.crt your-cert.pem
```

### **Log Locations**
- **Windows**: `%TEMP%\logs\`
- **Linux**: `/var/log/ecommerce-app/`
- **Docker**: `docker logs ecommerce-container`
- **Azure**: Application Insights or Log Stream
- **AWS**: CloudWatch Logs

---

## 📚 **ADDITIONAL RESOURCES**

### **Documentation**
- [.NET 8.0 Deployment Guide](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/)
- [Azure App Service Documentation](https://docs.microsoft.com/en-us/azure/app-service/)
- [AWS Elastic Beanstalk](https://docs.aws.amazon.com/elasticbeanstalk/)
- [Docker Documentation](https://docs.docker.com/)

### **Security Best Practices**
- [OWASP Security Guidelines](https://owasp.org/www-project-top-ten/)
- [Microsoft Security Best Practices](https://docs.microsoft.com/en-us/azure/security/)
- [SSL/TLS Configuration](https://ssl-config.mozilla.org/)

---

## 🎯 **DEPLOYMENT CHECKLIST**

- [ ] Environment variables configured
- [ ] Database connection tested
- [ ] SSL certificate installed
- [ ] Application built and published
- [ ] Health checks passing
- [ ] Security headers verified
- [ ] Monitoring configured
- [ ] Backup procedures tested
- [ ] Documentation updated
- [ ] Team notified of deployment

---

**🚀 Your E-Commerce application is now ready for production deployment!**
