# Monitoring & Alerting Integration Guide

## 📋 Overview

This guide explains how to integrate DocN's monitoring and alerting system with external tools and platforms.

**Version**: 1.0  
**Last Updated**: January 2026

---

## 🎯 Quick Setup

### 1. Enable Basic Monitoring

The basic monitoring is enabled by default. Access the following endpoints:

```bash
# Health checks
curl https://localhost:5211/health
curl https://localhost:5211/health/ready
curl https://localhost:5211/health/live

# Prometheus metrics
curl https://localhost:5211/metrics

# Alert metrics
curl https://localhost:5211/api/metrics/alerts

# RAG quality dashboard
curl https://localhost:5211/api/rag-quality/dashboard
```

### 2. Configure AlertManager

Edit `appsettings.json` or `appsettings.Development.json`:

```json
{
  "AlertManager": {
    "Enabled": true,
    "Endpoint": "http://localhost:9093"
  }
}
```

---

## 🔔 Email Notifications

### Setup Gmail

1. **Enable App Passwords** in your Google Account
2. **Configure in appsettings.json**:

```json
{
  "AlertManager": {
    "Enabled": true,
    "Routing": {
      "Email": {
        "Enabled": true,
        "SmtpHost": "smtp.gmail.com",
        "SmtpPort": 587,
        "SmtpUsername": "your-email@gmail.com",
        "SmtpPassword": "your-app-password",
        "UseSsl": true,
        "FromAddress": "your-email@gmail.com",
        "ToAddresses": [
          "alerts@yourcompany.com",
          "oncall@yourcompany.com"
        ]
      }
    }
  }
}
```

### Setup Other SMTP Providers

**Office 365**:
```json
{
  "SmtpHost": "smtp.office365.com",
  "SmtpPort": 587
}
```

**SendGrid**:
```json
{
  "SmtpHost": "smtp.sendgrid.net",
  "SmtpPort": 587,
  "SmtpUsername": "apikey",
  "SmtpPassword": "your-sendgrid-api-key"
}
```

**AWS SES**:
```json
{
  "SmtpHost": "email-smtp.us-east-1.amazonaws.com",
  "SmtpPort": 587,
  "SmtpUsername": "your-ses-username",
  "SmtpPassword": "your-ses-password"
}
```

---

## 💬 Slack Integration

### 1. Create Slack Webhook

1. Go to https://api.slack.com/apps
2. Create new app or select existing
3. Enable "Incoming Webhooks"
4. Add webhook to workspace
5. Copy webhook URL

### 2. Configure in appsettings.json

```json
{
  "AlertManager": {
    "Routing": {
      "Slack": {
        "Enabled": true,
        "WebhookUrl": "https://hooks.slack.com/services/YOUR/WEBHOOK/URL",
        "Channel": "#alerts",
        "Username": "DocN Alert Bot",
        "IconEmoji": ":alert:"
      }
    }
  }
}
```

### 3. Test Integration

```bash
curl -X POST https://localhost:5211/api/alerts/test \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test Alert",
    "description": "Testing Slack integration",
    "severity": "info"
  }'
```

---

## 🌐 Custom Webhook Integration

### PagerDuty

```json
{
  "AlertManager": {
    "Routing": {
      "Webhooks": [
        {
          "Name": "PagerDuty",
          "Url": "https://events.pagerduty.com/v2/enqueue",
          "Method": "POST",
          "Headers": {
            "Authorization": "Token token=your-pagerduty-token",
            "Content-Type": "application/json"
          },
          "Enabled": true
        }
      ]
    }
  }
}
```

### Opsgenie

```json
{
  "Webhooks": [
    {
      "Name": "Opsgenie",
      "Url": "https://api.opsgenie.com/v2/alerts",
      "Method": "POST",
      "Headers": {
        "Authorization": "GenieKey your-opsgenie-api-key"
      },
      "Enabled": true
    }
  ]
}
```

### Microsoft Teams

```json
{
  "Webhooks": [
    {
      "Name": "Teams",
      "Url": "https://outlook.office.com/webhook/YOUR-WEBHOOK-URL",
      "Method": "POST",
      "Enabled": true
    }
  ]
}
```

### Discord

```json
{
  "Webhooks": [
    {
      "Name": "Discord",
      "Url": "https://discord.com/api/webhooks/YOUR-WEBHOOK-URL",
      "Method": "POST",
      "Enabled": true
    }
  ]
}
```

---

## 📊 Prometheus & Grafana

### 1. Setup Prometheus

**prometheus.yml**:
```yaml
global:
  scrape_interval: 15s
  evaluation_interval: 15s

scrape_configs:
  - job_name: 'docn'
    static_configs:
      - targets: ['localhost:5211']
    metrics_path: '/metrics'
```

### 2. Setup AlertManager

**alertmanager.yml**:
```yaml
global:
  resolve_timeout: 5m

route:
  group_by: ['alertname', 'severity']
  group_wait: 10s
  group_interval: 10s
  repeat_interval: 12h
  receiver: 'default'
  routes:
    - match:
        severity: critical
      receiver: 'critical'
    - match:
        severity: warning
      receiver: 'warning'

receivers:
  - name: 'default'
    slack_configs:
      - api_url: 'YOUR_SLACK_WEBHOOK_URL'
        channel: '#alerts'
        
  - name: 'critical'
    pagerduty_configs:
      - service_key: 'YOUR_PAGERDUTY_KEY'
    
  - name: 'warning'
    email_configs:
      - to: 'team@yourcompany.com'
```

### 3. Configure Alert Rules

**alert_rules.yml**:
```yaml
groups:
  - name: docn_alerts
    interval: 30s
    rules:
      - alert: HighCPU
        expr: rate(process_cpu_usage[5m]) > 0.9
        for: 5m
        labels:
          severity: critical
          component: system
        annotations:
          summary: "High CPU usage detected"
          description: "CPU usage is above 90% for 5 minutes"
          runbook: "See docs/ALERTING_RUNBOOK.md#1-highcpu"
      
      - alert: HighMemory
        expr: process_memory_usage_bytes / process_memory_limit_bytes > 0.9
        for: 5m
        labels:
          severity: critical
          component: system
        annotations:
          summary: "High memory usage detected"
          description: "Memory usage is above 90% for 5 minutes"
          runbook: "See docs/ALERTING_RUNBOOK.md#2-highmemory"
      
      - alert: HighErrorRate
        expr: rate(http_server_requests_total{status=~"5.."}[5m]) > 0.05
        for: 5m
        labels:
          severity: critical
          component: api
        annotations:
          summary: "High error rate detected"
          description: "Error rate is above 5% for 5 minutes"
          runbook: "See docs/ALERTING_RUNBOOK.md#4-higherrorrate"
```

### 4. Setup Grafana Dashboard

Import the DocN dashboard JSON:

```json
{
  "dashboard": {
    "title": "DocN Monitoring",
    "panels": [
      {
        "title": "Request Rate",
        "targets": [
          {
            "expr": "rate(http_server_requests_total[5m])"
          }
        ]
      },
      {
        "title": "Error Rate",
        "targets": [
          {
            "expr": "rate(http_server_requests_total{status=~\"5..\"}[5m])"
          }
        ]
      },
      {
        "title": "P95 Latency",
        "targets": [
          {
            "expr": "histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))"
          }
        ]
      },
      {
        "title": "RAG Quality - Faithfulness",
        "targets": [
          {
            "expr": "ragas_faithfulness_score"
          }
        ]
      }
    ]
  }
}
```

---

## 🔍 OpenTelemetry Integration

### Jaeger Tracing

**appsettings.json**:
```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:4317"
  }
}
```

**docker-compose.yml**:
```yaml
services:
  jaeger:
    image: jaegertracing/all-in-one:latest
    ports:
      - "16686:16686"  # UI
      - "4317:4317"    # OTLP gRPC
      - "4318:4318"    # OTLP HTTP
```

### Zipkin

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://localhost:9411/api/v2/spans"
  }
}
```

---

## 🚀 Kubernetes Integration

### Deploy with Monitoring

**deployment.yaml**:
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: docn-server
  annotations:
    prometheus.io/scrape: "true"
    prometheus.io/port: "5211"
    prometheus.io/path: "/metrics"
spec:
  replicas: 3
  selector:
    matchLabels:
      app: docn-server
  template:
    metadata:
      labels:
        app: docn-server
    spec:
      containers:
      - name: docn-server
        image: docn:latest
        ports:
        - containerPort: 5211
          name: http
        env:
        - name: AlertManager__Enabled
          value: "true"
        - name: AlertManager__Endpoint
          value: "http://alertmanager:9093"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 5211
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 5211
          initialDelaySeconds: 5
          periodSeconds: 5
```

### ServiceMonitor for Prometheus Operator

```yaml
apiVersion: monitoring.coreos.com/v1
kind: ServiceMonitor
metadata:
  name: docn-server
spec:
  selector:
    matchLabels:
      app: docn-server
  endpoints:
  - port: http
    path: /metrics
    interval: 30s
```

---

## 📱 Mobile Notifications

### Pushover

```json
{
  "Webhooks": [
    {
      "Name": "Pushover",
      "Url": "https://api.pushover.net/1/messages.json",
      "Method": "POST",
      "Headers": {
        "Content-Type": "application/x-www-form-urlencoded"
      },
      "Enabled": true
    }
  ]
}
```

### Twilio SMS

```json
{
  "Webhooks": [
    {
      "Name": "Twilio",
      "Url": "https://api.twilio.com/2010-04-01/Accounts/YOUR_ACCOUNT_SID/Messages.json",
      "Method": "POST",
      "Headers": {
        "Authorization": "Basic BASE64(ACCOUNT_SID:AUTH_TOKEN)"
      },
      "Enabled": true
    }
  ]
}
```

---

## 🧪 Testing

### Test Email Notifications

```bash
# Configure email in appsettings.json, then:
curl -X POST https://localhost:5211/api/alerts/test \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Email Test",
    "description": "Testing email notifications",
    "severity": "warning"
  }'
```

### Test Slack Notifications

```bash
curl -X POST https://localhost:5211/api/alerts/test \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Slack Test",
    "description": "Testing Slack integration",
    "severity": "info"
  }'
```

### Test Webhook

```bash
curl -X POST https://localhost:5211/api/alerts/test \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Webhook Test",
    "description": "Testing webhook integration",
    "severity": "critical"
  }'
```

---

## 🔧 Troubleshooting

### Alerts Not Sending

1. Check if AlertManager is enabled:
```bash
curl https://localhost:5211/api/alerts/active
```

2. Check logs:
```bash
docker logs docn-server | grep -i alert
```

3. Verify configuration:
```bash
cat appsettings.json | grep -A 20 AlertManager
```

### Email Not Working

- Verify SMTP credentials
- Check firewall/security group for port 587
- Enable "Less secure app access" for Gmail
- Use app-specific password for Gmail with 2FA

### Slack Not Working

- Verify webhook URL is correct
- Check if webhook is active in Slack app settings
- Test webhook directly with curl

---

## 📚 Additional Resources

- [Alerting Runbook](./ALERTING_RUNBOOK.md)
- [RAG Quality Guide](./RAG_QUALITY_GUIDE.md)
- [Prometheus Documentation](https://prometheus.io/docs/)
- [Grafana Tutorials](https://grafana.com/tutorials/)

---

**Support**: For issues with integrations, check logs and consult the troubleshooting section above.
