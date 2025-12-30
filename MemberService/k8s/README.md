# Kubernetes Deployment for MemberService

This directory contains Kubernetes manifests for deploying MemberService to a Kubernetes cluster.

## Prerequisites

- Kubernetes cluster (v1.19+)
- kubectl configured to access your cluster
- Docker registry access (if using private registry)

## Quick Start

1. **Update Secrets**: Modify the `jwt-secret-key` and `db-password` in `deployment.yml` with your actual values.

2. **Deploy PostgreSQL**:
   ```bash
   kubectl apply -f k8s/postgres-deployment.yml
   ```

3. **Wait for PostgreSQL to be ready**:
   ```bash
   kubectl get pods -l app=memberservice-db
   kubectl wait --for=condition=ready pod -l app=memberservice-db --timeout=300s
   ```

4. **Run Database Migrations**:
   ```bash
   # Get the database pod name
   DB_POD=$(kubectl get pods -l app=memberservice-db -o jsonpath='{.items[0].metadata.name}')

   # Run migrations (you'll need to build and push the migration image first)
   kubectl run migration-job --image=memberservice:latest --restart=Never --rm -it -- /bin/bash
   ```

5. **Deploy MemberService**:
   ```bash
   kubectl apply -f k8s/deployment.yml
   ```

6. **Check deployment status**:
   ```bash
   kubectl get pods
   kubectl get services
   kubectl get ingress
   ```

## Configuration

### Environment Variables

The deployment uses ConfigMaps and Secrets for configuration:

- **ConfigMap**: `memberservice-config` - Non-sensitive configuration
- **Secret**: `memberservice-secrets` - Sensitive data (JWT keys, passwords)

### Updating Configuration

To update configuration without redeployment:

```bash
# Update ConfigMap
kubectl edit configmap memberservice-config

# Update Secret (base64 encoded)
kubectl edit secret memberservice-secrets
```

## Scaling

### Horizontal Scaling

```bash
# Scale to 3 replicas
kubectl scale deployment memberservice --replicas=3
```

### Vertical Scaling

Update the `resources` section in `deployment.yml`:

```yaml
resources:
  requests:
    memory: "256Mi"
    cpu: "200m"
  limits:
    memory: "1Gi"
    cpu: "1000m"
```

## Monitoring

### Health Checks

The deployment includes:
- **Liveness Probe**: `/api/health` - Restarts container if unhealthy
- **Readiness Probe**: `/api/health` - Removes from service if not ready

### Logs

```bash
# View application logs
kubectl logs -l app=memberservice

# Follow logs
kubectl logs -l app=memberservice -f

# View database logs
kubectl logs -l app=memberservice-db
```

## Troubleshooting

### Common Issues

1. **Pods not starting**:
   ```bash
   kubectl describe pod <pod-name>
   kubectl logs <pod-name>
   ```

2. **Database connection issues**:
   ```bash
   kubectl exec -it <db-pod-name> -- psql -U memberservice -d memberservice_prod
   ```

3. **Ingress not working**:
   ```bash
   kubectl get ingress
   kubectl describe ingress memberservice-ingress
   ```

### Cleanup

```bash
# Remove all resources
kubectl delete -f k8s/

# Remove PVC (WARNING: This deletes data)
kubectl delete pvc postgres-pvc
```

## Security Considerations

- Change default passwords in production
- Use strong JWT secret keys (32+ characters)
- Consider using TLS for ingress
- Implement network policies for pod-to-pod communication
- Use RBAC for Kubernetes access control
- Regularly update base images for security patches

## Production Deployment

For production deployment, consider:

- Using external PostgreSQL (AWS RDS, Azure Database, etc.)
- Implementing proper TLS certificates
- Setting up monitoring (Prometheus, Grafana)
- Configuring log aggregation (ELK stack)
- Implementing backup strategies
- Setting up CI/CD pipelines for automated deployment