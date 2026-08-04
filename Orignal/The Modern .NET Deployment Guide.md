# The Modern .NET Deployment Guide
[Original Post](https://medium.com/@mwaseemzakir/2872665cad7c)
An application running on a developer’s laptop is not finished.

Deployment exposes everything local development politely hides:

Configuration mistakes
Missing environment variables
File permissions
Network rules
TLS certificates
Database migrations
Logging problems
Health checks
Resource limits
Startup failures
Rollback challenges
Writing the application is only one part of the job. You also need a reliable way to build, configure, deploy, monitor, and recover it.

Here is a practical path for shipping a modern .NET application.

Step 1: Produce a Release Build
Start by creating a proper release build:

dotnet restore
dotnet build --configuration Release --no-restore
dotnet test --configuration Release --no-build
dotnet publish --configuration Release --output ./publish
Your deployment pipeline should fail immediately when the application cannot build or its tests fail.

Never build an application manually on the production server. Production should receive a tested and versioned artifact produced by your pipeline.

Step 2: Manage Configuration Correctly
ASP.NET Core can load configuration from several sources:

appsettings.json
Environment-specific configuration files
Environment variables
Command-line arguments
Secret stores
Cloud configuration systems
Do not commit production secrets to your repository.

Use a secret manager, protected CI/CD variables, platform configuration settings, or another controlled mechanism.

Configuration should also be separated by environment. Development, staging, and production rarely use the same databases, external services, logging levels, or security settings.

Step 3: Containerize the Application
A traditional multi-stage Dockerfile may look like this:

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MyApp.Api.dll"]
Adjust the target framework, application name, and image version to match your project.

Why Multi-Stage Builds Matter
The SDK image contains compilers, build tools, and everything required to produce the application.

The runtime image contains only what is required to run it.

Using separate build and runtime stages helps reduce:

Final image size
Unnecessary production tooling
Download and deployment time
Production attack surface
The production container should contain the application — not your entire development environment.

Step 4: Consider SDK Container Publishing
Modern .NET can build container images directly through the SDK:

dotnet publish --os linux --arch x64 /t:PublishContainer
This is useful when you want container publishing without maintaining a Dockerfile.

A Dockerfile remains valuable when your image requires:

Custom operating-system packages
Additional files
Specialized build steps
Detailed control over layers
Non-standard runtime configuration
Neither approach is universally better. Choose the simplest option that gives you the control your application actually needs.

Step 5: Run as a Non-Root User
Containers should not run with unnecessary privileges.

Use the conventions supported by the selected .NET runtime image and verify:

File ownership
Exposed ports
Writable directories
Mounted volumes
Secret permissions
Runtime user permissions
If the application only needs to write temporary files to one directory, do not give it permission to write everywhere.

Security should be built into the image and deployment process — not added as a final checkbox.

Step 6: Add Health Checks
A health endpoint can report whether the application and its dependencies are healthy.

builder.Services.AddHealthChecks()
    .AddNpgSql(
        builder.Configuration.GetConnectionString("Database")!);

app.MapHealthChecks("/health");
When your hosting platform supports them, separate the following concepts:

Liveness: Is the process still alive?
Readiness: Can the application currently receive traffic?
Startup: Has initialization completed successfully?
These checks should not always behave identically.

For example, a temporary database outage should not necessarily cause an orchestrator to restart a perfectly healthy application process repeatedly.

A restart cannot repair an unavailable external database. Sometimes it only creates more noise.

Step 7: Add Observability
A production application needs three major forms of telemetry :

Logs
Metrics
Traces
Logs
Logs capture detailed events with useful context.

A good log should help answer questions such as:

What happened?
When did it happen?
Which user or request was affected?
Which service produced the event?
What exception occurred?
Metrics
Metrics are numeric measurements collected over time.

Useful examples include:

Request duration
Error rate
Request count
Queue length
Database connection usage
CPU usage
Memory consumption
Traces
Traces show the path of a request across services and external dependencies.

They become especially useful when one request passes through multiple APIs, databases, message brokers, and background workers.

OpenTelemetry provides a common way to instrument .NET applications and export telemetry to supported observability platforms.

Whatever tools you choose, never log:

Passwords
Access tokens
API keys
Connection-string secrets
Sensitive personal information
Observability should help you investigate incidents without creating a new security incident.

Step 8: Build a CI Pipeline
A basic GitHub Actions workflow might look like this:

name : build
on:
  push:
    branches: [main]
  pull_request:
jobs:
  build-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: "10.0.x"
      - run: dotnet restore
      - run: dotnet build \
          --configuration Release \
          --no-restore
      - run: dotnet test \
          --configuration Release \
          --no-build
Adjust the .NET version and commands to match your project.

Your pipeline can later include:

Code formatting checks
Static analysis
Security scanning
Dependency scanning
Container builds
Integration tests
Artifact publishing
Deployment
Post-deployment smoke tests
Begin with a reliable build and test process. Add more controls as the project and team grow.

Step 9: Build and Publish a Container
The pipeline can build the application image and publish it to a container registry:

- name: Log in to registry
  uses: docker/login-action@v3
  with:
    registry: ghcr.io
    username: ${{ github.actor }}
    password: ${{ secrets.GITHUB_TOKEN }}
- name: Build and push
  uses: docker/build-push-action@v6
  with:
    context: .
    push: true
    tags: ghcr.io/your-org/your-app:${{ github.sha }}
Use immutable tags such as the commit SHA.

The latest tag is convenient for humans but vague during incident response.

If production is running image latest, which version is actually deployed?

With an immutable tag, you can connect a running container to a specific commit, build, and release.

Step 10: Handle Database Migrations
Database migrations require their own deployment strategy.

Common approaches include:

Running migrations as a separate deployment step
Using a dedicated migration job
Running a controlled migration application
Applying reviewed SQL scripts
Using EF Core migrations with operational safeguards
Avoid allowing every application replica to race against the others to migrate the database during startup.

Pay special attention to migrations that:

Add indexes to large tables
Change column types
Add non-null columns
Rebuild tables
Remove columns
Delete data
Lock heavily used resources
A migration that finishes instantly on your local database may behave very differently against a production table containing millions of rows.

Schema deployment is an operational change — not merely generated code.

Step 11: Choose a Hosting Model
There is no single hosting model that is right for every .NET application.

Choose according to your application’s size, traffic, budget, compliance needs, and the team’s operational experience.

Azure App Service
A good option for teams wanting managed web hosting with straightforward deployment and integration with the Microsoft ecosystem.

It can reduce the amount of infrastructure your team needs to manage directly.

AWS
AWS can be a strong choice when the organization already uses its services, networking, security controls, and operational tooling.

The best cloud is often the one your team already understands.

Managed Container Platforms
Platforms such as Railway, Render, Fly.io, and similar services can reduce setup work for small applications, prototypes, and early-stage products.

They allow teams to benefit from containers without immediately managing an entire container platform.

Kubernetes
Kubernetes is useful when an organization genuinely needs:

Container orchestration
Scheduling
Automated scaling
Service discovery
Deployment policies
Workload isolation
A shared internal platform
Kubernetes is not a badge of seriousness.

It is a powerful operational system with a real cost in infrastructure, knowledge, maintenance, and debugging.

Do not introduce it only because large companies use it.

VPS
A VPS can be affordable, flexible, and perfectly suitable for many applications.

However, you become responsible for:

Operating-system updates
Firewall configuration
Reverse proxy
TLS certificates
Backups
Process management
Monitoring
Security hardening
Disaster recovery
Cheap infrastructure can quickly become expensive responsibility.

A VPS is a good choice when you understand that you are purchasing control — not freedom from operations.

Step 12: Use a Reverse Proxy
A reverse proxy or managed load balancer commonly sits in front of the .NET application.

Popular options include:

Nginx
Caddy
Apache
YARP
Cloud-managed load balancers
Typical responsibilities include:

TLS termination
Host-based routing
Compression
Request-size limits
Static-file delivery
Forwarded headers
Load balancing
Configure forwarded headers correctly so the application understands the original request scheme, host, and client information.

Without correct forwarded-header configuration, the application may incorrectly believe an HTTPS request arrived through HTTP or may record the proxy address instead of the original client address.

Step 13: Plan Rollback
A deployment process is incomplete without a rollback strategy.

Possible approaches include:

Redeploying the previous image
Blue-green deployment
Canary deployment
Rolling deployment
Feature flags
Backward-compatible database changes
Rolling back application code is usually easier than rolling back a database.

For database changes, use an expand-and-contract strategy:

Add the new schema without removing the old schema.
Deploy code that supports both versions.
Migrate the existing data.
Move reads and writes to the new schema.
Remove the old schema in a later deployment.
This requires more patience, but production rewards patience far more generously than cleverness.

Step 14: Add Smoke Tests
After deployment, verify the application’s critical behavior.

At minimum, confirm that:

The application starts
The health endpoint responds
The database connection works
Authentication works
One important read operation succeeds
One safe write operation succeeds
Logs, metrics, and traces are arriving
A successful deployment command only proves that the deployment command succeeded.

It does not prove that users can use the application.

A Practical Deployment Path
Your infrastructure should grow with your application.

Small Application
A reasonable starting point may be:

GitHub Actions → Docker image → Managed platform or VPS → PostgreSQL → Basic logs and health checks

This is enough for many small products.

Growing Application
As the application grows, the process may become:

CI/CD → Container registry → Staging environment → Automated integration tests → Production → Metrics, traces, alerts, backups, and rollback

The main goal is to reduce uncertainty before changes reach users.

Distributed Platform
A larger platform may eventually require:

CI/CD → Signed images → Orchestrator → Secret management → Central telemetry → Progressive delivery → Automated policies

Do not begin at the final stage.

Infrastructure should grow with the problem. Starting with maximum complexity does not make an application production-ready. It only gives you more production problems to manage.

Final Thoughts
Modern .NET makes publishing an application relatively easy.

Operating it reliably is the harder part.

A strong deployment process should answer these questions:

What version is currently running?
Where does its configuration come from?
How are secrets protected?
How do we know the application is healthy?
How do we investigate failures?
How are database changes applied?
How quickly can we roll back?
What happens when one dependency becomes unavailable?
You do not need Kubernetes, dozens of tools, or an expensive cloud architecture on the first day.

You need a deployment process that is repeatable, observable, secure, and appropriate for your current scale.

Start simple. Automate the fragile parts. Add complexity only when the problem earns it.

You can find the maintained collection of related tools and learning resources in Docker, DevOps and Deployment.

Found this helpful? Leave a clap 👏 and share it with another .NET developer.

You might like these articles as well :
10 Real .NET Codebases Worth Your Weekend

30+ Tips for .NET Developers

30 More .NET Libraries Every Developer Should Know

There are 3 ways I can help you:
Enhance your .NET skills by subscribing to my YouTube Channel

Promote yourself to 10,000+ subscribers by sponsoring this newsletter

Have a software idea? Let’s turn it into a real product, Work With Me