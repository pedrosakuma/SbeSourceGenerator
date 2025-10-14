# Quick Start Guide: Setting up NuGet Publishing

This guide provides step-by-step instructions to set up automatic NuGet package publishing for SbeSourceGenerator.

## Prerequisites

- Repository admin access on GitHub
- Account on [NuGet.org](https://www.nuget.org/)

## Step 1: Create NuGet API Key

1. Go to [NuGet.org](https://www.nuget.org/) and sign in
2. Click on your username → **API Keys**
3. Click **Create**
4. Fill in the form:
   - **Key Name**: `SbeSourceGenerator-CI`
   - **Select Scopes**: Check `Push` and `Push new packages and package versions`
   - **Select Packages**:
     - **Glob Pattern**: `SbeSourceGenerator`
   - **Expiration**: Choose 365 days (or your preference)
5. Click **Create**
6. **Important**: Copy the API key immediately (it won't be shown again)

## Step 2: Add Secret to GitHub

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Enter:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Paste the API key from Step 1
5. Click **Add secret**

## Step 3: Test the Pipeline

### Option 1: Test CI (Build and Test)

1. Push any code change to the repository
2. Navigate to **Actions** tab
3. Watch the **CI** workflow run
4. Verify it completes successfully

### Option 2: Test CD (Publish)

**Warning**: This will actually publish to NuGet!

1. Create a test version tag:
   ```bash
   git tag v0.1.0-preview.2
   git push origin v0.1.0-preview.2
   ```
2. Go to **Releases** → **Draft a new release**
3. Select the tag you just created
4. Publish the release
5. Navigate to **Actions** tab
6. Watch the **CD - Publish to NuGet** workflow run
7. Verify the package appears on [NuGet.org](https://www.nuget.org/packages/SbeSourceGenerator/)

## Step 4: Verify Installation

Test that the package can be installed:

```bash
dotnet new console -n TestSbeGenerator
cd TestSbeGenerator
dotnet add package SbeSourceGenerator --version 0.1.0-preview.2
dotnet build
```

## Common Issues

### Issue: "Package already exists"

**Solution**: Use a new version number. NuGet doesn't allow overwriting existing versions.

### Issue: "Authentication failed"

**Solution**: Check that:
1. The API key is correctly copied
2. The secret name is exactly `NUGET_API_KEY`
3. The API key hasn't expired

### Issue: Build fails in CI

**Solution**: 
1. Run `dotnet build` locally to identify the issue
2. Fix the build errors
3. Push the fix

## Next Steps

1. Update version in `src/SbeCodeGenerator/SbeSourceGenerator.csproj`
2. Follow [Semantic Versioning](https://semver.org/) for version numbers
3. Create releases for stable versions
4. Monitor package statistics on NuGet.org

## Security Best Practices

✅ **DO**:
- Use repository secrets for API keys
- Set API key expiration
- Limit API key scope to specific packages
- Rotate keys periodically

❌ **DON'T**:
- Commit API keys to the repository
- Share API keys in issues or PRs
- Use the same API key for multiple projects
- Give API keys broader permissions than needed

## Resources

- [Full CI/CD Documentation](./CICD_PIPELINE.md)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [NuGet Publishing Guide](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)

---

**Questions?** Open an issue on GitHub or check the full [CI/CD Pipeline Documentation](./CICD_PIPELINE.md)
