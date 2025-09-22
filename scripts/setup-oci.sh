#!/bin/bash

# Oracle Cloud Infrastructure Setup Script
# This script helps automate the setup of OCI resources for the E-Commerce application

set -e

echo "🌥️  Oracle Cloud Infrastructure Setup Script"
echo "============================================="

# Check if OCI CLI is installed
if ! command -v oci &> /dev/null; then
    echo "❌ OCI CLI not found. Please install it first:"
    echo "   curl -L https://raw.githubusercontent.com/oracle/oci-cli/master/scripts/install/install.sh | bash"
    exit 1
fi

echo "✅ OCI CLI found"

# Check if user is authenticated
if ! oci iam user get --user-id $(oci iam user list --query 'data[0].id' --raw-output) &> /dev/null; then
    echo "❌ OCI CLI not configured. Please run 'oci setup config' first"
    exit 1
fi

echo "✅ OCI CLI configured"

# Get user input
read -p "Enter your compartment name (or press Enter for root compartment): " COMPARTMENT_NAME
read -p "Enter your application name [ecommerce-app]: " APP_NAME
APP_NAME=${APP_NAME:-ecommerce-app}

echo ""
echo "🔍 Getting OCI information..."

# Get tenancy information
TENANCY_OCID=$(oci iam tenancy get --query 'data.id' --raw-output)
TENANCY_NAMESPACE=$(oci os ns get --query 'data' --raw-output)
USER_OCID=$(oci iam user list --query 'data[0].id' --raw-output)
REGION=$(oci iam region-subscription list --query 'data[?("is-home-region")].name | [0]' --raw-output)

echo "   Tenancy OCID: $TENANCY_OCID"
echo "   Tenancy Namespace: $TENANCY_NAMESPACE"
echo "   User OCID: $USER_OCID"
echo "   Home Region: $REGION"

# Get or create compartment
if [ -z "$COMPARTMENT_NAME" ]; then
    COMPARTMENT_OCID=$TENANCY_OCID
    echo "   Using root compartment"
else
    COMPARTMENT_OCID=$(oci iam compartment list --name "$COMPARTMENT_NAME" --query 'data[0].id' --raw-output 2>/dev/null || echo "")
    
    if [ -z "$COMPARTMENT_OCID" ] || [ "$COMPARTMENT_OCID" == "null" ]; then
        echo "   Creating compartment: $COMPARTMENT_NAME"
        COMPARTMENT_OCID=$(oci iam compartment create \
            --compartment-id $TENANCY_OCID \
            --name "$COMPARTMENT_NAME" \
            --description "Compartment for $APP_NAME application" \
            --query 'data.id' \
            --raw-output)
        
        echo "   Waiting for compartment to be active..."
        sleep 10
    fi
    
    echo "   Compartment OCID: $COMPARTMENT_OCID"
fi

# Create VCN if it doesn't exist
VCN_NAME="${APP_NAME}-vcn"
VCN_OCID=$(oci network vcn list --compartment-id $COMPARTMENT_OCID --display-name "$VCN_NAME" --query 'data[0].id' --raw-output 2>/dev/null || echo "")

if [ -z "$VCN_OCID" ] || [ "$VCN_OCID" == "null" ]; then
    echo "🌐 Creating Virtual Cloud Network..."
    VCN_OCID=$(oci network vcn create \
        --compartment-id $COMPARTMENT_OCID \
        --display-name "$VCN_NAME" \
        --cidr-block "10.0.0.0/16" \
        --dns-label "${APP_NAME}vcn" \
        --query 'data.id' \
        --raw-output)
    
    echo "   Waiting for VCN to be available..."
    sleep 10
else
    echo "✅ Using existing VCN: $VCN_NAME"
fi

echo "   VCN OCID: $VCN_OCID"

# Create Internet Gateway
IGW_NAME="${APP_NAME}-igw"
IGW_OCID=$(oci network internet-gateway list --compartment-id $COMPARTMENT_OCID --vcn-id $VCN_OCID --display-name "$IGW_NAME" --query 'data[0].id' --raw-output 2>/dev/null || echo "")

if [ -z "$IGW_OCID" ] || [ "$IGW_OCID" == "null" ]; then
    echo "🌐 Creating Internet Gateway..."
    IGW_OCID=$(oci network internet-gateway create \
        --compartment-id $COMPARTMENT_OCID \
        --vcn-id $VCN_OCID \
        --display-name "$IGW_NAME" \
        --is-enabled true \
        --query 'data.id' \
        --raw-output)
    
    sleep 5
else
    echo "✅ Using existing Internet Gateway: $IGW_NAME"
fi

# Update default route table
ROUTE_TABLE_OCID=$(oci network vcn get --vcn-id $VCN_OCID --query 'data."default-route-table-id"' --raw-output)

echo "🛣️  Updating route table..."
oci network route-table update \
    --rt-id $ROUTE_TABLE_OCID \
    --route-rules '[{
        "destination": "0.0.0.0/0",
        "destinationType": "CIDR_BLOCK",
        "networkEntityId": "'$IGW_OCID'"
    }]' \
    --force > /dev/null

# Create public subnet
SUBNET_NAME="${APP_NAME}-public-subnet"
SUBNET_OCID=$(oci network subnet list --compartment-id $COMPARTMENT_OCID --vcn-id $VCN_OCID --display-name "$SUBNET_NAME" --query 'data[0].id' --raw-output 2>/dev/null || echo "")

if [ -z "$SUBNET_OCID" ] || [ "$SUBNET_OCID" == "null" ]; then
    echo "🌐 Creating public subnet..."
    
    # Get availability domain
    AD_NAME=$(oci iam availability-domain list --compartment-id $COMPARTMENT_OCID --query 'data[0].name' --raw-output)
    
    SUBNET_OCID=$(oci network subnet create \
        --compartment-id $COMPARTMENT_OCID \
        --vcn-id $VCN_OCID \
        --display-name "$SUBNET_NAME" \
        --cidr-block "10.0.1.0/24" \
        --availability-domain "$AD_NAME" \
        --dns-label "${APP_NAME}subnet" \
        --query 'data.id' \
        --raw-output)
    
    sleep 10
else
    echo "✅ Using existing subnet: $SUBNET_NAME"
    AD_NAME=$(oci network subnet get --subnet-id $SUBNET_OCID --query 'data."availability-domain"' --raw-output)
fi

echo "   Subnet OCID: $SUBNET_OCID"
echo "   Availability Domain: $AD_NAME"

# Update security list to allow HTTP traffic
SECURITY_LIST_OCID=$(oci network vcn get --vcn-id $VCN_OCID --query 'data."default-security-list-id"' --raw-output)

echo "🔒 Updating security list..."
oci network security-list update \
    --security-list-id $SECURITY_LIST_OCID \
    --ingress-security-rules '[
        {
            "isStateless": false,
            "protocol": "6",
            "source": "0.0.0.0/0",
            "tcpOptions": {
                "destinationPortRange": {
                    "max": 8080,
                    "min": 8080
                }
            }
        },
        {
            "isStateless": false,
            "protocol": "6",
            "source": "0.0.0.0/0",
            "tcpOptions": {
                "destinationPortRange": {
                    "max": 22,
                    "min": 22
                }
            }
        }
    ]' \
    --force > /dev/null

echo ""
echo "🎉 Oracle Cloud Infrastructure setup completed!"
echo ""
echo "📋 GitHub Secrets Configuration:"
echo "================================="
echo "Copy these values to your GitHub repository secrets:"
echo ""
echo "OCI_USER_OCID=$USER_OCID"
echo "OCI_TENANCY_OCID=$TENANCY_OCID"
echo "OCI_TENANCY_NAMESPACE=$TENANCY_NAMESPACE"
echo "OCI_REGION=$REGION"
echo "OCI_COMPARTMENT_OCID=$COMPARTMENT_OCID"
echo "OCI_SUBNET_OCID=$SUBNET_OCID"
echo "OCI_AVAILABILITY_DOMAIN=$AD_NAME"
echo ""
echo "⚠️  You still need to manually configure:"
echo "  - OCI_FINGERPRINT (from your API key)"
echo "  - OCI_PRIVATE_KEY (your private key content)"
echo "  - OCI_USERNAME (your OCI username)"
echo "  - OCI_AUTH_TOKEN (generated auth token)"
echo "  - Database connection strings"
echo "  - Application secrets (Brevo, PayPal, etc.)"
echo ""
echo "📖 For detailed setup instructions, see docs/ORACLE_CLOUD_SETUP.md"