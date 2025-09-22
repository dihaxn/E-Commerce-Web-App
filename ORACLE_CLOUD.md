# 🌥️ Oracle Cloud Deployment

This project now supports deployment to Oracle Cloud Infrastructure (OCI) Free Tier!

## 🚀 Quick Start

1. **Sign up for Oracle Cloud Free Tier**: [oracle.com/cloud/free](https://oracle.com/cloud/free)
2. **Follow the setup guide**: See [docs/ORACLE_CLOUD_SETUP.md](docs/ORACLE_CLOUD_SETUP.md)
3. **Run the setup script**: `bash scripts/setup-oci.sh`
4. **Configure GitHub secrets** with your OCI credentials
5. **Deploy**: Push to `main` branch or trigger workflow manually

## 📁 Files Added

- `.github/workflows/oci-deploy.yml` - Oracle Cloud deployment workflow
- `docs/ORACLE_CLOUD_SETUP.md` - Detailed setup instructions
- `scripts/setup-oci.sh` - Automated OCI resource setup script

## 🔧 Deployment Options

The main CI/CD workflow now supports multiple cloud providers:

### Automatic Deployment

- **Oracle Cloud**: Default for all pushes to `main` and `develop`
- **Azure**: Available via manual workflow dispatch

### Manual Deployment

Go to **Actions** > **CI/CD Pipeline** > **Run workflow** and choose:

- **Environment**: staging or production
- **Cloud Provider**: oracle, azure, or skip

## 🌟 Oracle Cloud Free Tier Benefits

- **Container Instances**: Up to 3,000 OCPU hours/month
- **Always Free Compute**: 2 AMD VMs + 1/8 OCPU each
- **Storage**: 200 GB block storage + 10 GB object storage
- **Database**: 2 Autonomous Databases (20 GB each)
- **Networking**: Complete VCN setup included

## 📊 What Gets Deployed

- **Containerized Application**: Your ASP.NET Core app in Docker
- **Container Registry**: Images stored in Oracle Container Registry
- **Container Instances**: Serverless container hosting
- **Health Monitoring**: Automatic health checks
- **Public Access**: Public IP with HTTP access on port 8080
- **Environment Variables**: All app settings configured
- **Auto-scaling**: Container instances scale based on demand

## 🔍 Monitoring

After deployment, you can monitor your application:

1. **OCI Console**: Developer Services > Container Instances
2. **Application Logs**: Available in the container instance details
3. **Metrics**: CPU, memory, and network usage
4. **Health Checks**: Automatic health monitoring on `/health` endpoint

## 💰 Cost Management

- **Free Tier Monitoring**: Track usage in OCI Console
- **Resource Cleanup**: Old images automatically cleaned up
- **Efficient Sizing**: Optimized resource allocation for free tier
- **Auto-shutdown**: Option to schedule container shutdown for cost savings

## 🔧 Configuration

### Required Environment Variables

All application settings are automatically configured:

- Database connections
- Email service (Brevo)
- Payment processing (PayPal)
- Security settings
- HTTPS/SSL configuration

### Scaling Configuration

- **Staging**: 0.5 OCPU, 4 GB RAM
- **Production**: 1.5 OCPU, 8 GB RAM
- **Auto-scaling**: Based on CPU and memory usage

## 🛠️ Troubleshooting

Common issues and solutions:

1. **Authentication errors**: Check OCI credentials in GitHub secrets
2. **Network issues**: Verify security list allows port 8080
3. **Container start failures**: Check application logs in OCI Console
4. **Resource limits**: Monitor free tier usage

For detailed troubleshooting, see the [setup guide](docs/ORACLE_CLOUD_SETUP.md).

## 🚀 Next Steps

After successful deployment:

1. **Custom Domain**: Set up a custom domain with OCI Load Balancer
2. **SSL Certificate**: Configure HTTPS with Let's Encrypt
3. **Database**: Set up Oracle Autonomous Database
4. **Monitoring**: Configure application monitoring and alerts
5. **Backup**: Set up automated backups

---

**Need help?** Check the [Oracle Cloud Setup Guide](docs/ORACLE_CLOUD_SETUP.md) for detailed instructions!
