Your Setup Summary
ServiceYour SituationApproachSQL ServerSSMS + SQLEXPRESS local✅ Already running — just verifyRabbitMQErlang + RabbitMQ installed locallyRun natively, no DockerRedisNothing installedDocker containerElasticsearchNothingDocker containerKibanaNothingDocker container.NET SDKVersion 10 installed⚠️ Important — see below

⚠️ Critical: .NET SDK Version 10
The implementation plan says .NET 8, but you have SDK 10. Do not install .NET 8 alongside it — unnecessary. Instead, tell the agent to target net10.0 everywhere.
Tell the agent this rule upfront:

"Use <TargetFramework>net10.0</TargetFramework> in every .csproj file. Use EF Core 9.x or 10.x packages, not 8.x. All other package versions stay the same unless there's a conflict."

The project code itself doesn't change — .NET 10 is fully compatible with everything in the plan.
Verify your SDK:
bashdotnet --version
# Should show 10.x.x

dotnet --list-sdks
# Should show 10.x.x listed

1. SQL Server — Verify It's Running
Open Services (Win+R → services.msc) and confirm SQL Server (SQLEXPRESS) is Running. Or just run:
bash# In a terminal — if this returns a result, SQL Server is up
sqlcmd -S localhost\SQLEXPRESS -E -Q "SELECT @@VERSION"
You do NOT need to create any databases manually. EF Core migrations will create them automatically when the agent runs dotnet ef database update for each service. The databases IFMS_Identity, IFMS_Stations, IFMS_Inventory, etc. will all be created by the agent.
The only thing to confirm: your Windows user XI\mohit has permission to create databases. If you're the machine owner this is already true. Verify once:
sql-- Run this in SSMS
SELECT IS_SRVROLEMEMBER('sysadmin')
-- Should return 1

2. RabbitMQ — Run Locally (No Docker)
Since you have Erlang + RabbitMQ installed, just start it and enable the management plugin.
Start RabbitMQ (run as Administrator):
bash# Option 1 — if installed as a Windows Service
net start RabbitMQ

# Option 2 — if not set up as a service, run manually
# Navigate to your RabbitMQ sbin folder (usually):
cd "C:\Program Files\RabbitMQ Server\rabbitmq_server-x.x.x\sbin"
rabbitmq-server.bat
Enable the Management UI plugin (one-time setup):
bash# Run as Administrator from the sbin folder
rabbitmq-plugins.bat enable rabbitmq_management
# Restart RabbitMQ after enabling
net stop RabbitMQ
net start RabbitMQ
Verify it's working:
Open browser → http://localhost:15672
Login: guest / guest
You should see the RabbitMQ management dashboard. If you see it, RabbitMQ is fully ready.

Note: The agent's code will create the exchanges and queues automatically on startup via the RabbitMqPublisher setup code — you do not need to manually create ifms.events exchange or any queues through the UI. The app creates them on first run.

Important — update your .env for local RabbitMQ:
envRABBITMQ_HOST=localhost
RABBITMQ_USER=guest
RABBITMQ_PASS=guest

3. Redis — Docker (One Command)
bashdocker run -d \
  --name ifms-redis \
  -p 6379:6379 \
  --restart unless-stopped \
  redis:7-alpine
Verify:
bashdocker exec -it ifms-redis redis-cli ping
# Should return: PONG
That's it. Redis needs nothing else configured.

4. Elasticsearch + Kibana — Docker
This is the most memory-hungry part. Elasticsearch needs at least 2GB RAM free.
Run Elasticsearch:
bashdocker run -d \
  --name ifms-elasticsearch \
  -p 9200:9200 \
  -p 9300:9300 \
  -e "discovery.type=single-node" \
  -e "xpack.security.enabled=false" \
  -e "ES_JAVA_OPTS=-Xms512m -Xmx512m" \
  --restart unless-stopped \
  docker.elastic.co/elasticsearch/elasticsearch:8.11.0
Run Kibana (wait ~30 seconds for Elasticsearch to start first):
bashdocker run -d \
  --name ifms-kibana \
  -p 5601:5601 \
  -e "ELASTICSEARCH_HOSTS=http://host.docker.internal:9200" \
  --restart unless-stopped \
  docker.elastic.co/kibana/kibana:8.11.0

host.docker.internal lets the Kibana container reach Elasticsearch running on your local machine's Docker network. This works on Windows Docker Desktop by default.

Verify Elasticsearch:
bash# In browser or curl:
http://localhost:9200
# Should return JSON with cluster info including "cluster_name"
Verify Kibana:
bashhttp://localhost:5601
# Should load the Kibana UI (takes ~60 seconds first time)
If Elasticsearch fails to start (common on Windows), you may need to increase the virtual memory setting:
bash# Run in PowerShell as Administrator
wsl -d docker-desktop
sysctl -w vm.max_map_count=262144
exit

5. Docker Desktop Itself
Since you're using Docker for Redis + Elasticsearch, make sure Docker Desktop is installed and running. You should see the Docker whale icon in your system tray.
Verify:


docker --version
docker ps
# Should list your running containers
```

---

## Full Pre-Flight Verification Checklist

Run through this before telling the agent to start:
```
✅ SQL Server
   - SSMS open and connected to localhost\SQLEXPRESS
   - Can create databases (sysadmin role confirmed)
   - No databases named IFMS_* exist yet (agent creates them fresh)

✅ RabbitMQ
   - http://localhost:15672 opens and shows dashboard
   - Login works with guest/guest
   - No existing exchanges/queues named ifms.* (agent creates them)

✅ Redis
   - docker ps shows ifms-redis is Up
   - redis-cli ping returns PONG

✅ Elasticsearch
   - http://localhost:9200 returns cluster JSON
   - No index pattern named ifms-logs-* yet (agent creates on first log write)

✅ Kibana
   - http://localhost:5601 loads UI

✅ .NET SDK
   - dotnet --version shows 10.x.x
   - You've told the agent to use net10.0 in all csproj files

✅ Node.js (for Angular)
   - node --version (need v18+ for Angular 17)
   - npm --version
   - If not installed: https://nodejs.org → LTS version

✅ Docker Desktop
   - docker ps works without errors
```

---

## One Thing to Tell the Agent at the Start

Paste this at the very top of your first message to the agent, before anything else:
```
ENVIRONMENT NOTES — READ BEFORE STARTING:

1. .NET SDK is version 10. Use <TargetFramework>net10.0</TargetFramework> 
   in ALL .csproj files. Use EF Core 9.x packages, not 8.x.

2. SQL Server: Running locally at localhost\SQLEXPRESS with Windows Auth.
   Connection string: Server=localhost\SQLEXPRESS;Database={DB_NAME};
   Trusted_Connection=True;TrustServerCertificate=True;

3. RabbitMQ: Running locally (not Docker). Host = localhost, port = 5672.
   Credentials: guest / guest

4. Redis: Running in Docker on localhost:6379. No password.

5. Elasticsearch: Running in Docker on localhost:9200. Security disabled.

6. Do NOT add sql-server or rabbitmq to docker-compose.yml — 
   they run locally. Only add redis, elasticsearch, kibana to docker-compose.

   
This prevents the agent from trying to spin up SQL Server and RabbitMQ in Docker, which would conflict with your local installations.