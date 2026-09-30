# CI/CD Demo: .NET + GitHub Actions + Docker Hub

A small ASP.NET Core (.NET 10) minimal API with a complete, real delivery pipeline:

**Test -> Build -> Security scan -> Push to Docker Hub -> Staging -> Manual approval -> Production -> Rollback**

## Repository layout

| Path | What it shows |
|------|---------------|
| `src/CicdDemo.Api` | The API (minimal API, `Program.cs`) |
| `tests/CicdDemo.Tests` | xUnit integration tests using `WebApplicationFactory` |
| `CicdDemo.sln` | Solution file so `dotnet restore/build/test` work from the repo root |
| `Dockerfile` | Multi-stage build (SDK image -> small runtime image), non-root, HEALTHCHECK |
| `.github/workflows/ci.yml` | **CI**: build + tests + Docker build check on every PR / branch push |
| `.github/workflows/cd.yml` | **CD**: test, build, Trivy scan, push to Docker Hub, staging, approval, production |
| `.github/workflows/rollback.yml` | Manual rollback to any previous commit |

## How GitHub Actions runs this (the short version)

1. You `git push`. GitHub sees the **event** and finds the matching **workflow** in `.github/workflows/`.
2. Each **job** in the workflow is handed to a **runner**: a fresh virtual machine (`runs-on: ubuntu-latest`).
3. The runner executes the job's **steps** one by one: `actions/checkout` downloads the code, `setup-dotnet` installs the SDK, then `dotnet test`, `docker build`, and so on.
4. Logs stream back to the Actions tab. When the job ends, the VM is **destroyed**. Jobs do not share files; use artifacts, or rebuild.

## How "deploy" works here

Every build is pushed to Docker Hub as an immutable tag: `<user>/cicd-demo:<commit-sha>`.
Environments are **moving tags** that point at one of those images:

| Environment | Tag | When it moves |
|-------------|-----|---------------|
| Staging | `:staging` | Automatically after a successful build |
| Production | `:prod` and `:latest` | After a reviewer approves |

The same image (same digest) is promoted through the environments: *build once, deploy many*.

## 0. One-time setup

1. Create a Docker Hub account, then an **access token**: Account Settings > Personal access tokens > *Read & Write*.
2. Create a GitHub repo named `cicd-demo` and push this code.
3. In the repo: **Settings > Secrets and variables > Actions > New repository secret**:
   - `DOCKERHUB_USERNAME`: your Docker Hub username (lowercase)
   - `DOCKERHUB_TOKEN`: the access token from step 1
4. **Settings > Environments**: create `staging` and `production`. On `production`, enable **Required reviewers** and add yourself. This is the manual approval gate (Continuous *Delivery*).

## 1. Demo: Continuous Integration

```bash
git checkout -b feature/demo
# break a test on purpose: in tests/CicdDemo.Tests/ApiTests.cs change HttpStatusCode.OK to HttpStatusCode.Created
git commit -am "break test" && git push -u origin feature/demo
```
Open a Pull Request. CI runs and **fails**, so the code can't be merged. Open the failed run, show the runner logs, download the `test-results` artifact. Fix it and push again: it turns green.

## 2. Demo: Continuous Delivery pipeline

Merge the PR into `main`. Open the **Actions** tab and watch `CD`:

1. `test` runs the tests.
2. `build-scan-push` builds the image, Trivy scans it (fails on HIGH/CRITICAL), then pushes `<user>/cicd-demo:<commit-sha>` to Docker Hub.
3. `deploy-staging` moves the `:staging` tag to that image.
4. `deploy-prod` **waits for your approval**. Approve it and `:prod` / `:latest` move to that image.

Open your repository on hub.docker.com and refresh the Tags tab to see the tags appear.

## 3. Demo: run what was deployed

```bash
docker run --rm -p 8080:8080 <user>/cicd-demo:staging
curl localhost:8080/         # {"message":"Hello from CI/CD demo","version":"<commit-sha>",...}
curl localhost:8080/health
```
The `version` field is the commit SHA baked into the image, so you can prove exactly which commit is running. Try the same with `:prod`.

## 4. Demo: rollback

1. Make and merge a second change so two versions exist on Docker Hub, and approve it to production.
2. Go to **Actions > Rollback > Run workflow** and paste the **previous** full commit SHA.
3. After approval, `:prod` points at the old image again. `docker pull <user>/cicd-demo:prod` and `curl` to confirm the version changed back.

## Run locally

```bash
dotnet test
ASPNETCORE_URLS=http://localhost:8080 dotnet run --project src/CicdDemo.Api    # http://localhost:8080
# or
docker compose up --build
```
