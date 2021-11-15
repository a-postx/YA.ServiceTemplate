# YA.ServiceTemplate

[RU](README.md), [EN](README.en.md)

## About

Web API service built with distributed architecture in mind. The application shows how to use .Net platform for creating microservice systems and could be a quick start point for creating a new application.

Application is ready for
  - using your favourite ORM-framwork instead of in-memory repository
  - enabling authentification based on JWT-tokens

## Build with

- Platform: .Net Core 6
- Communication: message queues (MassTransit + RabbitMQ)
- Logging and tracing: Serilog, ELK, ApplicationInsights
- Secrets: AWS Parameter Store
- Model validation: FluentValidation
- Documentation: Swagger (OpenAPI v3)


## Features

### Design
Domain-driven design (DDD) separates business logic from core and support code. Command layer separates business logic from presentation layer (ex. web request or message queue event). All support infrastructure is implemented as internal services.

### Idempotency
Application returns cached response on any duplicate HTTP-call (request with the same idempotency key) to protect data consistency.

### State
The application is stateless so can be used by orchestration systems like Kubernetes for scaling out.

### Upgrade
The application has API versioning for easy standalone upgrade. Dependent applications will be able to use a previous API version in HTTP calls and message bus messages until version retirement.

### Logging
The application is configured with the following items:

- all HTTP-requests contains CorrelationID headers
- all message bus messages has CorrelationID identifier

Structured log events contains unified correlation identifier so changes caused by a single HTTP-request or message queue message can be easily identified and tracked.

### Monitoring
The application contains health checks page (/status) and metrics page (/metrics) to be used by monitoring systems that works with pull model (ex. Prometheus).

### Secrets
Sensitive application options are stored as environment variables and on external secrets storage (AWS Systems Manager).

### Documentation
Web-API has Swagger-based documentation (/swagger) for a frontend developers.

## Configuration and launch

The application requires a bit configuration before launch.

### Get resources

The following resources are required:
- AWS Secrets storage instance
- RabbitMQ message broker instance

Optional:
- Logz.IO access token
- Application Insights instrumentation key

#### Secrets storage
 * Go to https://aws.amazon.com and create a free account
 * Open up https://console.aws.amazon.com and go to Security Credentials (IAM)
   - Create a new access policy containing the following access rights
   ```json
   {
    "Version": "2012-10-17",
    "Statement": [
        {
            "Action": [
                "ssm:DescribeParameters",
                "ssm:GetParameter",
                "ssm:GetParametersByPath",
                "ssm:GetParameters",
                "ssm:GetParameter"
            ],
            "Effect": "Allow",
            "Resource": "arn:aws:ssm:*:*:*"
        }
      ]
    }
   ```
   - Createa new user account with access based on the created policy (choose "programmatic access") to get Access Key and Secret Key

#### RabbitMQ message broker
Register on https://www.cloudamqp.com/ and create instance with a free plan to get a connection string (amqp://{login}:{password}@{host}/{vhost})

#### LogzIO logging
If you would like to use Logstash and Kibana for log management then you can register on https://logz.io (choose one of the EU datacenters) to get a free access token

#### Application Insights telemetry
If you would like to deploy application on Miscrosoft Azure then you may benefit from Application Insights telemetry. Go to https://portal.azure.com, open up Application Insights and add new resource to get instrumentation key

### Configure
* Go to https://console.aws.amazon.com, choose "Europe (Frankfurt) eu-central-1" AWS region and open up Systems Manager service
* Go to "Applicatin Management -> Parameter Store" and add the following parameters with the values you get on the previous steps:
  - /development/messagebushost
  - /development/messagebusvhost
  - /development/messagebuslogin
  - /development/messagebuspassword
  - /development/logziotoken (optional)
  - /development/appinsightsinstrumentationkey (optional)

* Add AWS keys to environment variables on a machine you're going to launch the application on:
  - AWS_ACCESS_KEY_ID: <AccessKey>
  - AWS_SECRET_ACCESS_KEY: <SecretKey>

### Launch

Choose 'Kestrel' launch profile and run the app.

## License
[MIT](https://github.com/a-postx/YA.ServiceTemplate/blob/master/LICENSE)