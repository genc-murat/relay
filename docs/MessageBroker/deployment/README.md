# Deployment Guides

Comprehensive deployment guides for Relay.MessageBroker applications across different platforms and environments.

## Available Guides

### [Docker Deployment](./DOCKER_DEPLOYMENT.md)
Deploy using Docker and Docker Compose for development and small-scale production deployments.

**Best for:**
- Local development
- Small-scale production
- Quick prototyping
- Single-server deployments

**Includes:**
- Complete docker-compose.yml with all dependencies
- RabbitMQ, PostgreSQL, Redis setup
- Monitoring stack (Prometheus, Grafana, Jaeger)
- Health checks and logging
- Scaling with Docker Compose

### [Kubernetes Deployment](./KUBERNETES_DEPLOYMENT.md)
Deploy using Kubernetes and Helm charts for enterprise-scale production deployments.

**Best for:**
- Enterprise production environments
- High availability requirements
- Auto-scaling needs
- Multi-region deployments

**Includes:**
- Complete Helm chart structure
- Production-ready configurations
- Auto-scaling (HPA)
- Pod Disruption Budgets
- Service mesh integration
- Monitoring with Prometheus Operator

### Azure Deployment
Deploy to Azure using Azure Container Apps, AKS, or App Service.

**Coming soon** - See [Azure-specific considerations](#azure-considerations) below.

### AWS Deployment
Deploy to AWS using ECS, EKS, or Elastic Beanstalk.

**Coming soon** - See [AWS-specific considerations](#aws-considerations) below.

## Quick Start

### Docker (Development)

```bash
# Clone repository
git clone https://github.com/your-org/relay-app.git
cd relay-app

# Start with Docker Compose
docker-compose up -d

# Check health
curl http://localhost:8080/health
```

### Kubernetes (Production)

```bash
# Add Helm repository
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update

# Install
helm install relay-app ./helm/relay-messagebroker \
  --namespace relay-prod \
  --values values-prod.yaml \
  --create-namespace

# Check status
kubectl get pods -n relay-prod
```

## Platform Comparison

| Feature | Docker Compose | Kubernetes | Azure | AWS |
|---------|---------------|------------|-------|-----|
| **Complexity** | Low | High | Medium | Medium |
| **Scalability** | Limited | Excellent | Excellent | Excellent |
| **Cost** | Low | Variable | Variable | Variable |
| **Management** | Manual | Automated | Managed | Managed |
| **Best For** | Dev/Small | Enterprise | Azure Users | AWS Users |

## Architecture Patterns

### Single Region Deployment

```
┌─────────────────────────────────────────┐
│           Load Balancer                  │
└────────────┬────────────────────────────┘
             │
    ┌────────┴────────┐
    │                 │
┌───▼────┐      ┌────▼───┐
│ App 1  │      │ App 2  │
└───┬────┘      └────┬───┘
    │                │
    └────────┬───────┘
             │
    ┌────────▼────────┐
    │   RabbitMQ      │
    │   PostgreSQL    │
    │   Redis         │
    └─────────────────┘
```

### Multi-Region Deployment

```
┌──────────────┐         ┌──────────────┐
│  Region 1    │         │  Region 2    │
│              │         │              │
│  ┌────────┐  │         │  ┌────────┐  │
│  │ App    │  │◄───────►│  │ App    │  │
│  └───┬────┘  │         │  └───┬────┘  │
│      │       │         │      │       │
│  ┌───▼────┐  │         │  ┌───▼────┐  │
│  │RabbitMQ│  │◄───────►│  │RabbitMQ│  │
│  └────────┘  │         │  └────────┘  │
└──────────────┘         └──────────────┘
```

## Azure Considerations

### Azure Container Apps

**Pros:**
- Fully managed
- Auto-scaling
- Built-in ingress
- Pay-per-use

**Cons:**
- Limited customization
- Newer service

**Setup:**
```bash
# Create resource group
az group create --name relay-rg --location eastus

# Create container app environment
az containerapp env create \
  --name relay-env \
  --resource-group relay-rg \
  --location eastus

# Deploy app
az containerapp create \
  --name relay-app \
  --resource-group relay-rg \
  --environment relay-env \
  --image your-registry.azurecr.io/relay-app:2.0.0 \
  --target-port 8080 \
  --ingress external \
  --min-replicas 3 \
  --max-replicas 10
```

### Azure Kubernetes Service (AKS)

**Pros:**
- Full Kubernetes features
- Azure integration
- Mature service

**Cons:**
- More complex
- Higher management overhead

**Setup:**
```bash
# Create AKS cluster
az aks create \
  --resource-group relay-rg \
  --name relay-aks \
  --node-count 3 \
  --enable-addons monitoring \
  --generate-ssh-keys

# Get credentials
az aks get-credentials --resource-group relay-rg --name relay-aks

# Deploy with Helm
helm install relay-app ./helm/relay-messagebroker \
  --namespace relay-prod \
  --values values-azure.yaml
```

### Azure Service Bus Integration

When using Azure Service Bus:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options =>
    {
        options.BrokerType = MessageBrokerType.AzureServiceBus;
        options.AzureServiceBus = new AzureServiceBusOptions
        {
            ConnectionString = configuration["AzureServiceBus:ConnectionString"],
            EntityType = AzureEntityType.Topic
        };
    });
```

## AWS Considerations

### Amazon ECS (Elastic Container Service)

**Pros:**
- AWS-native
- Good integration
- Simpler than EKS

**Cons:**
- Less flexible than Kubernetes
- AWS-specific

**Setup:**
```bash
# Create ECS cluster
aws ecs create-cluster --cluster-name relay-cluster

# Register task definition
aws ecs register-task-definition --cli-input-json file://task-definition.json

# Create service
aws ecs create-service \
  --cluster relay-cluster \
  --service-name relay-app \
  --task-definition relay-app:1 \
  --desired-count 3 \
  --launch-type FARGATE
```

### Amazon EKS (Elastic Kubernetes Service)

**Pros:**
- Full Kubernetes
- AWS integration
- Portable

**Cons:**
- Complex setup
- Higher cost

**Setup:**
```bash
# Create EKS cluster
eksctl create cluster \
  --name relay-cluster \
  --region us-east-1 \
  --nodegroup-name relay-nodes \
  --node-type t3.medium \
  --nodes 3

# Deploy with Helm
helm install relay-app ./helm/relay-messagebroker \
  --namespace relay-prod \
  --values values-aws.yaml
```

### AWS SQS/SNS Integration

When using AWS SQS/SNS:

```csharp
services.AddMessageBrokerWithProfile(
    MessageBrokerProfile.Production,
    options =>
    {
        options.BrokerType = MessageBrokerType.AwsSqsSns;
        options.AwsSqsSns = new AwsSqsSnsOptions
        {
            Region = "us-east-1",
            AccessKeyId = configuration["AWS:AccessKeyId"],
            SecretAccessKey = configuration["AWS:SecretAccessKey"]
        };
    });
```

## Monitoring Setup

### Prometheus and Grafana

All deployment guides include Prometheus and Grafana setup. Access dashboards:

- **Prometheus:** http://localhost:9090 (Docker) or via ingress (K8s)
- **Grafana:** http://localhost:3000 (Docker) or via ingress (K8s)

### Application Insights (Azure)

```csharp
services.AddApplicationInsightsTelemetry(configuration["ApplicationInsights:ConnectionString"]);
```

### CloudWatch (AWS)

```csharp
services.AddAWSService<IAmazonCloudWatch>();
services.AddLogging(builder =>
{
    builder.AddAWSProvider();
});
```

## Security Best Practices

### Secrets Management

**Docker:**
- Use Docker secrets
- Environment variables
- External secret stores

**Kubernetes:**
- Kubernetes Secrets
- External Secrets Operator
- Azure Key Vault / AWS Secrets Manager

**Azure:**
- Azure Key Vault
- Managed Identity

**AWS:**
- AWS Secrets Manager
- IAM Roles

### Network Security

1. **Use private networks** for internal communication
2. **Enable TLS** for all external connections
3. **Implement network policies** in Kubernetes
4. **Use security groups** in cloud platforms
5. **Enable DDoS protection**

### Access Control

1. **Use RBAC** in Kubernetes
2. **Implement IAM** in cloud platforms
3. **Enable audit logging**
4. **Use service accounts** with minimal permissions
5. **Regular security audits**

## Performance Tuning

### Resource Allocation

**Development:**
- CPU: 500m - 1000m
- Memory: 512Mi - 1Gi

**Production:**
- CPU: 1000m - 2000m
- Memory: 1Gi - 2Gi

### Connection Pooling

```yaml
connectionPool:
  minPoolSize: 5
  maxPoolSize: 50
  connectionTimeout: 5s
```

### Batch Processing

```yaml
batching:
  maxBatchSize: 1000
  flushInterval: 50ms
  enableCompression: true
```

## Disaster Recovery

### Backup Strategy

1. **Database backups** - Daily automated backups
2. **Configuration backups** - Version control
3. **Message broker backups** - Persistent volumes
4. **Monitoring data** - Long-term storage

### Recovery Procedures

1. **Database restore** from backup
2. **Redeploy application** from version control
3. **Restore message broker** state
4. **Verify health checks**
5. **Resume traffic**

## Cost Optimization

### Docker
- Use resource limits
- Optimize image sizes
- Share infrastructure

### Kubernetes
- Use spot instances
- Enable cluster autoscaler
- Right-size resources
- Use namespace quotas

### Azure
- Use reserved instances
- Enable autoscaling
- Use Azure Hybrid Benefit
- Monitor with Cost Management

### AWS
- Use Savings Plans
- Enable autoscaling
- Use Spot Instances
- Monitor with Cost Explorer

## Support

For deployment issues:
- 📖 [Documentation](https://docs.relay.dev)
- 🐛 [GitHub Issues](https://github.com/your-org/relay/issues)
- 💬 [Discussions](https://github.com/your-org/relay/discussions)
- 📧 [Email Support](mailto:support@relay.dev)

## Contributing

See [CONTRIBUTING.md](../../../CONTRIBUTING.md) for information on contributing deployment guides.
