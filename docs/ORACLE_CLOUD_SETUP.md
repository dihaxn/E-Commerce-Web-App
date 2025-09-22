# Oracle Cloud Infrastructure (OCI) Deployment Setup Guide

This guide will help you set up deployment of your E-Commerce application to Oracle Cloud Infrastructure Free Tier.

## 🌥️ Oracle Cloud Free Tier Resources

The free tier includes:

- **Container Instances**: Up to 3,000 OCPU hours and 18,000 GB hours per month
- **Compute**: 2 AMD-based VMs (1/8 OCPU each) + 1 GB memory each
- **Storage**: 200 GB block storage, 10 GB object storage
- **Database**: 2 Autonomous Databases (1 OCPU each, 20 GB storage each)
- **Networking**: 1 VCN, 2 subnets, 1 internet gateway, 1 NAT gateway

## 📋 Prerequisites

1. **Oracle Cloud Account**: Sign up at [oracle.com/cloud/free](https://oracle.com/cloud/free)
2. **OCI Tenancy**: Access to your Oracle Cloud tenancy
3. **GitHub Repository**: This repository with GitHub Actions enabled

## 🔧 Step 1: Oracle Cloud Setup

### 1.1 Create a Compartment (Optional)

```bash
# In OCI Console, create a new compartment for your application
# Navigate to: Identity & Security > Compartments > Create Compartment
Name: ecommerce-app
Description: E-Commerce application resources
```

### 1.2 Set up Virtual Cloud Network (VCN)

```bash
# Navigate to: Networking > Virtual Cloud Networks > Create VCN
Name: ecommerce-vcn
CIDR Block: 10.0.0.0/16

# Create Public Subnet
Name: ecommerce-public-subnet
CIDR Block: 10.0.1.0/24
Route Table: Default Route Table
Security List: Default Security List
```

### 1.3 Configure Security Rules

Add ingress rules to the Default Security List:

```bash
# HTTP Traffic
Source: 0.0.0.0/0
Protocol: TCP
Port: 8080

# HTTPS Traffic (if needed)
Source: 0.0.0.0/0
Protocol: TCP
Port: 443
```

### 1.4 Create API Key for Authentication

```bash
# Generate API key pair
mkdir ~/.oci
openssl genrsa -out ~/.oci/oci_api_key.pem 2048
openssl rsa -pubout -in ~/.oci/oci_api_key.pem -out ~/.oci/oci_api_key_public.pem

# Add the public key to your OCI user
# Navigate to: Identity & Security > Users > [Your User] > API Keys > Add API Key
# Upload the public key file: oci_api_key_public.pem
```

### 1.5 Set up Container Registry

```bash
# Navigate to: Developer Services > Container Registry
# Create a repository (it will be created automatically when you push)
# Enable "Public" access if you want public images, or keep private for security
```

## 🔐 Step 2: GitHub Secrets Configuration

Add these secrets to your GitHub repository settings (`Settings > Secrets and variables > Actions`):

### Required Secrets:

```bash
# OCI Authentication
OCI_USER_OCID=ocid1.user.oc1..aaaaaaa...
OCI_TENANCY_OCID=ocid1.tenancy.oc1..aaaaaaa...
OCI_TENANCY_NAMESPACE=your-tenancy-namespace
OCI_FINGERPRINT=aa:bb:cc:dd:ee:ff:... (from API key)
OCI_PRIVATE_KEY=-----BEGIN RSA PRIVATE KEY-----...-----END RSA PRIVATE KEY-----
OCI_REGION=us-ashburn-1
OCI_COMPARTMENT_OCID=ocid1.compartment.oc1..aaaaaaa...
OCI_SUBNET_OCID=ocid1.subnet.oc1.iad.aaaaaaa...
OCI_AVAILABILITY_DOMAIN=ABCD:US-ASHBURN-AD-1

# Container Registry
OCI_USERNAME=your-oci-username
OCI_AUTH_TOKEN=your-auth-token

# Database Connections
OCI_STAGING_CONNECTION_STRING=Server=staging-db;Database=ecommerce;...
OCI_PRODUCTION_CONNECTION_STRING=Server=prod-db;Database=ecommerce;...

# Application Secrets (same as before)
BREVO_API_KEY=your-brevo-api-key
BREVO_SENDER_NAME=Your App Name
BREVO_SENDER_EMAIL=noreply@yourapp.com
PAYPAL_CLIENT_ID=your-paypal-client-id
PAYPAL_SECRET=your-paypal-secret
PAYPAL_URL=https://api.sandbox.paypal.com (or production URL)
COOKIE_ENCRYPTION_KEY=your-32-character-encryption-key
```

## 🔍 How to Find Required OCIDs

### User OCID:

```bash
# OCI Console: Identity & Security > Users > [Your User] > User Details
# Copy the OCID value
```

### Tenancy OCID & Namespace:

```bash
# OCI Console: Profile menu (top right) > Tenancy: [Your Tenancy Name]
# Copy both OCID and Object Storage Namespace
```

### Compartment OCID:

```bash
# OCI Console: Identity & Security > Compartments
# Click on your compartment and copy the OCID
```

### Subnet OCID:

```bash
# OCI Console: Networking > Virtual Cloud Networks > [Your VCN] > Subnets
# Click on your public subnet and copy the OCID
```

### Availability Domain:

```bash
# Use OCI CLI or Console to list ADs in your region
oci iam availability-domain list
# Use the first available AD name
```

### API Key Fingerprint:

```bash
# When you add the API key in OCI Console, it shows the fingerprint
# Or calculate it: openssl rsa -pubout -outform DER -in ~/.oci/oci_api_key.pem | openssl md5 -c
```

## 🗄️ Step 3: Database Setup (Optional)

If you want to use Oracle Autonomous Database:

### 3.1 Create Autonomous Database

```bash
# Navigate to: Oracle Database > Autonomous Database > Create Autonomous Database
Database Name: ecommerce-staging / ecommerce-production
Workload Type: Data Warehouse or Transaction Processing
Deployment Type: Shared Infrastructure
Choose "Always Free" for free tier
```

### 3.2 Get Connection String

```bash
# In the Autonomous Database details page:
# DB Connection > Download Wallet
# Use the connection string from tnsnames.ora
```

## 🚀 Step 4: Deploy Your Application

### 4.1 Trigger Deployment

```bash
# Push to main branch for production deployment
git push origin main

# Push to develop branch for staging deployment
git push origin develop

# Or use manual trigger:
# GitHub > Actions > Oracle Cloud Deployment > Run workflow
```

### 4.2 Monitor Deployment

```bash
# Check GitHub Actions logs for:
# - Docker image build and push to OCIR
# - Container instance creation
# - Health checks
# - Public IP assignment
```

### 4.3 Access Your Application

```bash
# After successful deployment, the logs will show:
# Application URL: http://[PUBLIC_IP]:8080
```

## 📊 Step 5: Monitoring and Management

### 5.1 OCI Console Monitoring

```bash
# Navigate to: Developer Services > Container Instances
# View your running instances, logs, and metrics
```

### 5.2 Application Logs

```bash
# In Container Instance details:
# Containers > [Your Container] > Logs
```

### 5.3 Resource Usage

```bash
# Navigate to: Governance & Administration > Account Management > Usage
# Monitor your free tier usage
```

## 🔧 Troubleshooting

### Common Issues:

1. **Authentication Errors**:

   - Verify all OCIDs are correct
   - Check API key fingerprint
   - Ensure private key is properly formatted

2. **Network Errors**:

   - Verify security list allows port 8080
   - Check subnet configuration
   - Ensure internet gateway is attached

3. **Container Start Failures**:

   - Check application logs in OCI Console
   - Verify environment variables
   - Check resource limits

4. **Database Connection Issues**:
   - Verify connection strings
   - Check database wallet configuration
   - Ensure database is running

## 💡 Cost Optimization Tips

1. **Use Free Tier Resources**: Stay within free tier limits
2. **Monitor Usage**: Regularly check usage reports
3. **Clean Up**: Remove unused resources
4. **Optimize Images**: Use multi-stage Docker builds
5. **Resource Sizing**: Use appropriate CPU/memory for your needs

## 🔄 Updating the Application

The deployment workflow automatically:

- Builds new Docker images on code changes
- Pushes to Oracle Container Registry
- Updates container instances with new images
- Performs health checks
- Cleans up old images

## 📚 Additional Resources

- [OCI Documentation](https://docs.oracle.com/en-us/iaas/)
- [Container Instances Guide](https://docs.oracle.com/en-us/iaas/Content/container-instances/home.htm)
- [OCI CLI Reference](https://docs.oracle.com/en-us/iaas/tools/oci-cli/latest/oci_cli_docs/)
- [Oracle Cloud Free Tier](https://www.oracle.com/cloud/free/)
