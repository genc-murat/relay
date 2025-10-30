# Kubernetes Deployment Guide

This guide covers deploying Relay.MessageBroker applications on Kubernetes using Helm charts.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Helm Chart Structure](#helm-chart-structure)
- [Installation](#installation)
- [Configuration](#configuration)
- [Monitoring](#monitoring)
- [Scaling](#scaling)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- Kubernetes 1.28+ cluster
- Helm 3.12+ installed
- kubectl configured
- Basic understanding of Kubernetes concepts

## Helm Chart Structure

```
relay-messagebroker/
├── Chart.yaml
├── values.yaml
├── values-dev.yaml
├── values-prod.yaml
├── templates/
│   ├── deployment.yaml
│   ├── service.yaml
│   ├── configmap.yaml
│   ├── secret.yaml
│   ├── hpa.yaml
│   ├── pdb.yaml
│   ├── servicemonitor.yaml
│   ├── ingress.yaml
│   └── _helpers.tpl
└── charts/
    ├── rabbitmq/
    ├── postgresql/
    └── redis/
```

### Chart.yaml

```yaml
apiVersion: v2
name: relay-messagebroker
description: Helm chart for Relay.MessageBroker application
type: application
version: 2.0.0
appVersion: "2.0.0"
keywords:
  - messaging
  - message-broker
  - microservices
maintainers:
  - name: Relay Team
    email: team@relay.dev
dependencies:
  - name: rabbitmq
    version: "12.10.0"
    repository: https://charts.bitnami.com/bitnami
    condition: rabbitmq.enabled
  - name: postgresql
    version: "13.2.24"
    repository: https://charts.bitnami.com/bitnami
    condition: postgresql.enabled
  - name: redis
    version: "18.6.1"
    repository: https://charts.bitnami.com/bitnami
    condition: redis.enabled
```

### values.yaml

```yaml
# Default values for relay-messagebroker

replicaCount: 3

image:
  repository: your-registry/relay-app
  pullPolicy: IfNotPresent
  tag: "2.0.0"

imagePullSecrets: []
nameOverride: ""
fullnameOverride: ""

serviceAccount:
  create: true
  annotations: {}
  name: ""

podAnnotations:
  prometheus.io/scrape: "true"
  prometheus.io/port: "8080"
  prometheus.io/path: "/metrics"

podSecurityContext:
  runAsNonRoot: true
  runAsUser: 1000
  fsGroup: 1000

securityContext:
  capabilities:
    drop:
    - ALL
  readOnlyRootFilesystem: true
  allowPrivilegeEscalation: false

service:
  type: ClusterIP
  port: 80
  targetPort: 8080
  annotations: {}

ingress:
  enabled: false
  className: "nginx"
  annotations:
    cert-manager.io/cluster-issuer: "letsencrypt-prod"
  hosts:
    - host: relay-app.example.com
      paths:
        - path: /
          pathType: Prefix
  tls:
    - secretName: relay-app-tls
      hosts:
        - relay-app.example.com

resources:
  limits:
    cpu: 1000m
    memory: 1Gi
  requests:
    cpu: 500m
    memory: 512Mi

autoscaling:
  enabled: true
  minReplicas: 3
  maxReplicas: 10
  targetCPUUtilizationPercentage: 70
  targetMemoryUtilizationPercentage: 80

nodeSelector: {}

tolerations: []

affinity:
  podAntiAffinity:
    preferredDuringSchedulingIgnoredDuringExecution:
      - weight: 100
        podAffinityTerm:
          labelSelector:
            matchExpressions:
              - key: app.kubernetes.io/name
                operator: In
                values:
                  - relay-messagebroker
          topologyKey: kubernetes.io/hostname

# Application Configuration
config:
  aspnetcore:
    environment: Production
    urls: "http://+:8080"
  
  messagebroker:
    brokerType: RabbitMQ
    profile: Production
  
  logging:
    level: Information

# RabbitMQ Configuration
rabbitmq:
  enabled: true
  auth:
    username: admin
    password: ""  # Set via secret
    existingPasswordSecret: rabbitmq-secret
  persistence:
    enabled: true
    size: 10Gi
  resources:
    limits:
      cpu: 1000m
      memory: 2Gi
    requests:
      cpu: 500m
      memory: 1Gi
  metrics:
    enabled: true
    serviceMonitor:
      enabled: true

# PostgreSQL Configuration
postgresql:
  enabled: true
  auth:
    username: relay
    password: ""  # Set via secret
    database: relay
    existingSecret: postgresql-secret
  primary:
    persistence:
      enabled: true
      size: 20Gi
    resources:
      limits:
        cpu: 1000m
        memory: 2Gi
      requests:
        cpu: 500m
        memory: 1Gi
  metrics:
    enabled: true
    serviceMonitor:
      enabled: true

# Redis Configuration
redis:
  enabled: true
  auth:
    enabled: true
    password: ""  # Set via secret
    existingSecret: redis-secret
  master:
    persistence:
      enabled: true
      size: 5Gi
    resources:
      limits:
        cpu: 500m
        memory: 1Gi
      requests:
        cpu: 250m
        memory: 512Mi
  metrics:
    enabled: true
    serviceMonitor:
      enabled: true

# Monitoring
monitoring:
  enabled: true
  serviceMonitor:
    enabled: true
    interval: 30s
    scrapeTimeout: 10s

# Pod Disruption Budget
podDisruptionBudget:
  enabled: true
  minAvailable: 2

# Health Checks
healthCheck:
  liveness:
    enabled: true
    path: /health/live
    initialDelaySeconds: 30
    periodSeconds: 10
    timeoutSeconds: 5
    failureThreshold: 3
  readiness:
    enabled: true
    path: /health/ready
    initialDelaySeconds: 10
    periodSeconds: 5
    timeoutSeconds: 3
    failureThreshold: 3

# Secrets
secrets:
  rabbitmq:
    password: ""  # Override in values-prod.yaml or use external secret
  postgresql:
    password: ""
  redis:
    password: ""
  encryption:
    key: ""
  jwt:
    secret: ""
```

### templates/deployment.yaml

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: {{ include "relay-messagebroker.fullname" . }}
  labels:
    {{- include "relay-messagebroker.labels" . | nindent 4 }}
spec:
  {{- if not .Values.autoscaling.enabled }}
  replicas: {{ .Values.replicaCount }}
  {{- end }}
  selector:
    matchLabels:
      {{- include "relay-messagebroker.selectorLabels" . | nindent 6 }}
  template:
    metadata:
      annotations:
        checksum/config: {{ include (print $.Template.BasePath "/configmap.yaml") . | sha256sum }}
        checksum/secret: {{ include (print $.Template.BasePath "/secret.yaml") . | sha256sum }}
        {{- with .Values.podAnnotations }}
        {{- toYaml . | nindent 8 }}
        {{- end }}
      labels:
        {{- include "relay-messagebroker.selectorLabels" . | nindent 8 }}
    spec:
      {{- with .Values.imagePullSecrets }}
      imagePullSecrets:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      serviceAccountName: {{ include "relay-messagebroker.serviceAccountName" . }}
      securityContext:
        {{- toYaml .Values.podSecurityContext | nindent 8 }}
      containers:
      - name: {{ .Chart.Name }}
        securityContext:
          {{- toYaml .Values.securityContext | nindent 12 }}
        image: "{{ .Values.image.repository }}:{{ .Values.image.tag | default .Chart.AppVersion }}"
        imagePullPolicy: {{ .Values.image.pullPolicy }}
        ports:
        - name: http
          containerPort: 8080
          protocol: TCP
        {{- if .Values.healthCheck.liveness.enabled }}
        livenessProbe:
          httpGet:
            path: {{ .Values.healthCheck.liveness.path }}
            port: http
          initialDelaySeconds: {{ .Values.healthCheck.liveness.initialDelaySeconds }}
          periodSeconds: {{ .Values.healthCheck.liveness.periodSeconds }}
          timeoutSeconds: {{ .Values.healthCheck.liveness.timeoutSeconds }}
          failureThreshold: {{ .Values.healthCheck.liveness.failureThreshold }}
        {{- end }}
        {{- if .Values.healthCheck.readiness.enabled }}
        readinessProbe:
          httpGet:
            path: {{ .Values.healthCheck.readiness.path }}
            port: http
          initialDelaySeconds: {{ .Values.healthCheck.readiness.initialDelaySeconds }}
          periodSeconds: {{ .Values.healthCheck.readiness.periodSeconds }}
          timeoutSeconds: {{ .Values.healthCheck.readiness.timeoutSeconds }}
          failureThreshold: {{ .Values.healthCheck.readiness.failureThreshold }}
        {{- end }}
        resources:
          {{- toYaml .Values.resources | nindent 12 }}
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: {{ .Values.config.aspnetcore.environment }}
        - name: ASPNETCORE_URLS
          value: {{ .Values.config.aspnetcore.urls }}
        envFrom:
        - configMapRef:
            name: {{ include "relay-messagebroker.fullname" . }}
        - secretRef:
            name: {{ include "relay-messagebroker.fullname" . }}
        volumeMounts:
        - name: tmp
          mountPath: /tmp
        - name: app-tmp
          mountPath: /app/tmp
      volumes:
      - name: tmp
        emptyDir: {}
      - name: app-tmp
        emptyDir: {}
      {{- with .Values.nodeSelector }}
      nodeSelector:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.affinity }}
      affinity:
        {{- toYaml . | nindent 8 }}
      {{- end }}
      {{- with .Values.tolerations }}
      tolerations:
        {{- toYaml . | nindent 8 }}
      {{- end }}
```

### templates/hpa.yaml

```yaml
{{- if .Values.autoscaling.enabled }}
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: {{ include "relay-messagebroker.fullname" . }}
  labels:
    {{- include "relay-messagebroker.labels" . | nindent 4 }}
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: {{ include "relay-messagebroker.fullname" . }}
  minReplicas: {{ .Values.autoscaling.minReplicas }}
  maxReplicas: {{ .Values.autoscaling.maxReplicas }}
  metrics:
  {{- if .Values.autoscaling.targetCPUUtilizationPercentage }}
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: {{ .Values.autoscaling.targetCPUUtilizationPercentage }}
  {{- end }}
  {{- if .Values.autoscaling.targetMemoryUtilizationPercentage }}
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: {{ .Values.autoscaling.targetMemoryUtilizationPercentage }}
  {{- end }}
{{- end }}
```

### templates/pdb.yaml

```yaml
{{- if .Values.podDisruptionBudget.enabled }}
apiVersion: policy/v1
kind: PodDisruptionBudget
metadata:
  name: {{ include "relay-messagebroker.fullname" . }}
  labels:
    {{- include "relay-messagebroker.labels" . | nindent 4 }}
spec:
  minAvailable: {{ .Values.podDisruptionBudget.minAvailable }}
  selector:
    matchLabels:
      {{- include "relay-messagebroker.selectorLabels" . | nindent 6 }}
{{- end }}
```

## Installation

### Add Helm Repository

```bash
# Add Bitnami repository for dependencies
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
```

### Install with Default Values

```bash
# Create namespace
kubectl create namespace relay

# Install chart
helm install relay-app ./relay-messagebroker \
  --namespace relay \
  --create-namespace
```

### Install with Custom Values

```bash
# Development
helm install relay-app ./relay-messagebroker \
  --namespace relay-dev \
  --values values-dev.yaml

# Production
helm install relay-app ./relay-messagebroker \
  --namespace relay-prod \
  --values values-prod.yaml
```

### Install with Secrets

```bash
# Create secrets first
kubectl create secret generic rabbitmq-secret \
  --from-literal=rabbitmq-password='<strong-password>' \
  --namespace relay-prod

kubectl create secret generic postgresql-secret \
  --from-literal=password='<strong-password>' \
  --namespace relay-prod

kubectl create secret generic redis-secret \
  --from-literal=redis-password='<strong-password>' \
  --namespace relay-prod

# Install chart
helm install relay-app ./relay-messagebroker \
  --namespace relay-prod \
  --values values-prod.yaml
```

## Configuration

### Production Values (values-prod.yaml)

```yaml
replicaCount: 5

image:
  repository: your-registry.azurecr.io/relay-app
  tag: "2.0.0"

resources:
  limits:
    cpu: 2000m
    memory: 2Gi
  requests:
    cpu: 1000m
    memory: 1Gi

autoscaling:
  enabled: true
  minReplicas: 5
  maxReplicas: 20
  targetCPUUtilizationPercentage: 60
  targetMemoryUtilizationPercentage: 70

ingress:
  enabled: true
  hosts:
    - host: relay-app.production.com
      paths:
        - path: /
          pathType: Prefix

rabbitmq:
  enabled: true
  replicaCount: 3
  persistence:
    size: 50Gi
  resources:
    limits:
      cpu: 2000m
      memory: 4Gi
    requests:
      cpu: 1000m
      memory: 2Gi

postgresql:
  enabled: true
  primary:
    persistence:
      size: 100Gi
    resources:
      limits:
        cpu: 2000m
        memory: 4Gi
      requests:
        cpu: 1000m
        memory: 2Gi

redis:
  enabled: true
  master:
    persistence:
      size: 20Gi
    resources:
      limits:
        cpu: 1000m
        memory: 2Gi
      requests:
        cpu: 500m
        memory: 1Gi
```

## Monitoring

### Prometheus ServiceMonitor

The chart automatically creates a ServiceMonitor when monitoring is enabled:

```yaml
monitoring:
  enabled: true
  serviceMonitor:
    enabled: true
    interval: 30s
```

### Grafana Dashboards

Import the provided Grafana dashboards:

```bash
kubectl create configmap relay-dashboards \
  --from-file=dashboards/ \
  --namespace monitoring
```

## Scaling

### Manual Scaling

```bash
# Scale deployment
kubectl scale deployment relay-app --replicas=10 -n relay-prod

# Scale via Helm
helm upgrade relay-app ./relay-messagebroker \
  --namespace relay-prod \
  --set replicaCount=10
```

### Auto-scaling

HPA is enabled by default in production. Monitor scaling:

```bash
# Watch HPA
kubectl get hpa -n relay-prod -w

# Describe HPA
kubectl describe hpa relay-app -n relay-prod
```

## Troubleshooting

### Check Pod Status

```bash
kubectl get pods -n relay-prod
kubectl describe pod <pod-name> -n relay-prod
kubectl logs <pod-name> -n relay-prod
```

### Check Services

```bash
kubectl get svc -n relay-prod
kubectl describe svc relay-app -n relay-prod
```

### Check Configuration

```bash
kubectl get configmap relay-app -n relay-prod -o yaml
kubectl get secret relay-app -n relay-prod -o yaml
```

### Debug Pod

```bash
kubectl exec -it <pod-name> -n relay-prod -- /bin/sh
```

## Best Practices

1. **Use namespaces** for environment isolation
2. **Enable RBAC** and service accounts
3. **Set resource limits** and requests
4. **Use Pod Disruption Budgets**
5. **Enable auto-scaling**
6. **Use anti-affinity** rules
7. **Enable monitoring** and alerting
8. **Use secrets** for sensitive data
9. **Regular backups** of persistent volumes
10. **Keep charts updated**
