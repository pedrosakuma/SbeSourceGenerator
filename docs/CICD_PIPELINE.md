# CI/CD Pipeline Documentation

Este documento descreve a configuração e funcionamento da pipeline de CI/CD para o projeto SbeSourceGenerator.

## Visão Geral

O projeto utiliza **GitHub Actions** para automação de CI/CD com três workflows:

1. **CI (Continuous Integration)** — Build, testes e validação de código em PRs e pushes
2. **Release** — Pipeline principal: tag push → build → test → GitHub Release → NuGet publish
3. **CD Manual** — Fallback para publicação manual no NuGet via workflow_dispatch

## Fluxo de Release

```
Desenvolvedor
  │
  ├─── Push/PR ─────────────────────────► CI Workflow (build + test)
  │
  └─── git tag v1.2.3 && git push tag ──► Release Workflow
                                              │
                                              ├── Build + Test
                                              ├── Pack NuGet (.nupkg)
                                              ├── Validate package
                                              ├── Create GitHub Release (com .nupkg anexado)
                                              └── Publish to NuGet.org
```

**Um único comando (`git push origin v1.2.3`) executa todo o pipeline de release automaticamente.**

## Workflow de CI

### Arquivo: `.github/workflows/ci.yml`

### Gatilhos

- **Push** nos branches: `master`, `main`, `develop`
- **Pull Requests** para os branches: `master`, `main`, `develop`
- **Manualmente** via workflow_dispatch

### Etapas

1. Checkout + Setup .NET 9.0
2. Restore dependencies
3. Build (Release)
4. Build + Run Tests
5. Pack NuGet (dry-run validation)
6. Upload artifacts (7 dias)

## Workflow de Release (Principal)

### Arquivo: `.github/workflows/release.yml`

### Gatilho

```yaml
on:
  push:
    tags:
      - 'v*'
```

Acionado automaticamente quando uma tag no formato `v*` é pushada (ex: `v1.0.1`, `v2.0.0-beta.1`).

### Etapas

1. Checkout + Setup .NET 9.0
2. Extract version from tag (`v1.0.1` → `1.0.1`)
3. Restore + Build (com `/p:Version`)
4. Build + Run Tests
5. Pack NuGet (com `/p:PackageVersion`)
6. Validate package (verifica `analyzers/dotnet/cs/`)
7. **Create GitHub Release** (com release notes geradas automaticamente + `.nupkg` anexado)
8. **Push to NuGet.org**
9. Upload artifacts (90 dias)

### Como Publicar uma Nova Versão

```bash
# 1. Atualize a versão no csproj, CHANGELOG.md e README.md
# 2. Faça commit e push das mudanças (via PR)
# 3. Após merge, crie e push a tag:
git tag v1.2.0
git push origin v1.2.0

# Pronto! O pipeline faz o resto automaticamente:
# ✅ Build + Test
# ✅ GitHub Release criada com .nupkg
# ✅ Pacote publicado no NuGet.org
```

## Workflow de CD Manual (Fallback)

### Arquivo: `.github/workflows/publish.yml`

### Gatilho

Apenas `workflow_dispatch` com input de versão. Use apenas como fallback em situações excepcionais (ex: republicação, correção de pacote).

⚠️ **Nota**: Este workflow NÃO cria GitHub Release. Prefira o fluxo de tag push.

### Execução Manual

1. Navegue até **Actions** → **CD - Manual NuGet Publish**
2. Clique em **"Run workflow"**
3. Digite a versão (ex: `1.0.1`)
4. Clique em **"Run workflow"**

## Configuração de Secrets

### NUGET_API_KEY

1. **Obter API Key no NuGet.org**
   - Account Settings → API Keys → Create
   - Key Name: `SbeSourceGenerator-CI`
   - Scopes: `Push` + `Push new packages and package versions`
   - Glob Pattern: `SbeSourceGenerator`

2. **Adicionar Secret no GitHub**
   - Repository → Settings → Secrets and variables → Actions
   - New repository secret: `NUGET_API_KEY` = (valor da API Key)

⚠️ **IMPORTANTE**: Nunca exponha a API Key em código, logs ou documentação pública!

## Versionamento Semântico

O projeto segue [Semantic Versioning 2.0.0](https://semver.org/):

```
MAJOR.MINOR.PATCH[-PRERELEASE]

Exemplos:
- 1.0.0         - Release estável
- 1.0.1         - Patch (correção de bugs)
- 1.1.0         - Minor (novos recursos, retrocompatível)
- 2.0.0         - Major (mudanças que quebram compatibilidade)
- 1.0.0-beta.1  - Pre-release
```

## Checklist de Release

- [ ] Atualizar CHANGELOG.md com as mudanças
- [ ] Atualizar versão em `SbeSourceGenerator.csproj`
- [ ] Atualizar versão no `README.md`
- [ ] Fazer merge de todas as PRs necessárias
- [ ] Executar testes localmente: `dotnet test`
- [ ] Criar e push tag: `git tag vX.Y.Z && git push origin vX.Y.Z`
- [ ] Verificar workflow na aba Actions
- [ ] Verificar GitHub Release criada com .nupkg
- [ ] Verificar pacote no NuGet.org

## Troubleshooting

### Tag criada mas Release não aparece

**Causa**: A tag precisa ser pushada (`git push origin vX.Y.Z`), não apenas criada localmente.

**Solução**: `git push origin vX.Y.Z`

### Erro: "Package already exists"

**Causa**: Versão já publicada no NuGet.org.

**Solução**: Use uma nova versão. O flag `--skip-duplicate` previne falha.

### Erro: "Authentication failed"

**Causa**: API Key inválida ou expirada.

**Solução**: Gere nova API Key no NuGet.org e atualize o secret.

### NuGet publicado sem GitHub Release

**Causa**: Workflow manual (publish.yml) não cria releases.

**Solução**: Use o fluxo de tag push (`release.yml`), ou crie a release manualmente via `gh release create vX.Y.Z`.

---

**Última Atualização**: 2026-04-10
**Versão do Documento**: 2.0
