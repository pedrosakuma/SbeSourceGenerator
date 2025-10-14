# CI/CD Pipeline Documentation

Este documento descreve a configuração e funcionamento da pipeline de CI/CD para o projeto SbeSourceGenerator.

## Visão Geral

O projeto utiliza **GitHub Actions** para automação de CI/CD com dois workflows principais:

1. **CI (Continuous Integration)** - Build, testes e validação de código
2. **CD (Continuous Deployment)** - Publicação automática no NuGet

## Workflow de CI

### Arquivo: `.github/workflows/ci.yml`

### Gatilhos

O workflow de CI é executado automaticamente nos seguintes eventos:

- **Push** nos branches: `master`, `main`, `develop`
- **Pull Requests** para os branches: `master`, `main`, `develop`
- **Manualmente** através do botão "Run workflow" na interface do GitHub Actions

### Etapas do Workflow

1. **Checkout do código**
   - Faz o clone do repositório
   
2. **Setup .NET**
   - Instala o SDK .NET 9.0.x
   
3. **Restore dependencies**
   - Restaura as dependências do projeto: `dotnet restore`
   
4. **Build**
   - Compila o projeto em modo Release: `dotnet build --configuration Release`
   
5. **Build Tests**
   - Compila os projetos de teste
   
6. **Test**
   - Executa todos os testes: `dotnet test --configuration Release`
   - Exibe os resultados dos testes no log
   
7. **Pack NuGet package (dry-run)**
   - Cria o pacote NuGet sem publicá-lo (validação)
   - Gera o arquivo `.nupkg`
   
8. **Upload build artifacts**
   - Faz upload do pacote NuGet gerado como artefato
   - Retenção: 7 dias
   - Útil para validação antes da publicação

### Como Visualizar Resultados

1. Navegue até a aba **Actions** no GitHub
2. Selecione o workflow **CI**
3. Clique na execução desejada para ver os logs detalhados
4. Os artefatos gerados podem ser baixados na seção "Artifacts"

## Workflow de CD (Publicação no NuGet)

### Arquivo: `.github/workflows/publish.yml`

### Gatilhos

O workflow de CD é executado automaticamente quando:

1. **Uma release é publicada no GitHub**
   - Ao criar uma nova release/tag no formato `vX.Y.Z` (ex: `v1.0.0`)
   - A versão é extraída automaticamente da tag

2. **Manualmente através do workflow_dispatch**
   - Permite executar manualmente especificando a versão
   - Útil para republicações ou correções

### Etapas do Workflow

1. **Checkout do código**
   - Faz o clone do repositório
   
2. **Setup .NET**
   - Instala o SDK .NET 9.0.x
   
3. **Extract version from tag**
   - Extrai a versão da tag (ex: `v1.0.0` → `1.0.0`)
   - Ou usa a versão fornecida manualmente
   
4. **Restore dependencies**
   - Restaura as dependências: `dotnet restore`
   
5. **Build**
   - Compila com a versão específica: `dotnet build /p:Version=X.Y.Z`
   
6. **Test**
   - Executa todos os testes para garantir qualidade
   
7. **Pack NuGet package**
   - Cria o pacote NuGet com a versão correta
   - Formato: `SbeSourceGenerator.X.Y.Z.nupkg`
   
8. **Push to NuGet**
   - Publica o pacote no NuGet.org
   - Utiliza a API Key armazenada nos secrets
   - Flag `--skip-duplicate` previne erros se a versão já existir
   
9. **Upload release artifacts**
   - Faz upload do pacote como artefato da execução
   - Retenção: 90 dias

### Como Publicar uma Nova Versão

#### Método 1: Criar uma Release (Recomendado)

1. Navegue até **Releases** no GitHub
2. Clique em **"Draft a new release"**
3. Crie uma nova tag no formato `vX.Y.Z` (ex: `v1.0.0`)
4. Preencha o título e descrição da release
5. Clique em **"Publish release"**
6. O workflow será executado automaticamente

#### Método 2: Execução Manual

1. Navegue até **Actions** → **CD - Publish to NuGet**
2. Clique em **"Run workflow"**
3. Selecione o branch
4. Digite a versão (ex: `1.0.0`)
5. Clique em **"Run workflow"**

## Configuração de Secrets

### NUGET_API_KEY

Para que a publicação no NuGet funcione, é necessário configurar a API Key:

#### 1. Obter API Key do NuGet.org

1. Acesse [NuGet.org](https://www.nuget.org/)
2. Faça login na sua conta
3. Vá em **Account Settings** → **API Keys**
4. Clique em **"Create"**
5. Configure:
   - **Key Name**: `SbeSourceGenerator-CI`
   - **Scopes**: Selecione `Push` e `Push new packages and package versions`
   - **Select Packages**: 
     - **Glob Pattern**: `SbeSourceGenerator`
   - **Expiration**: Escolha um período apropriado (ex: 365 dias)
6. Clique em **"Create"**
7. **COPIE A API KEY** (ela será exibida apenas uma vez)

#### 2. Adicionar Secret no GitHub

1. No repositório GitHub, vá em **Settings** → **Secrets and variables** → **Actions**
2. Clique em **"New repository secret"**
3. Configure:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Cole a API Key copiada do NuGet.org
4. Clique em **"Add secret"**

⚠️ **IMPORTANTE**: Nunca exponha a API Key em código, logs ou documentação pública!

## Versionamento Semântico

O projeto segue [Semantic Versioning 2.0.0](https://semver.org/):

```
MAJOR.MINOR.PATCH[-PRERELEASE]

Exemplos:
- 1.0.0       - Release estável
- 1.0.1       - Patch (correção de bugs)
- 1.1.0       - Minor (novos recursos, retrocompatível)
- 2.0.0       - Major (mudanças que quebram compatibilidade)
- 1.0.0-beta.1 - Pre-release
- 1.0.0-rc.1   - Release candidate
```

### Diretrizes de Versionamento

- **MAJOR**: Mudanças que quebram compatibilidade com versões anteriores
- **MINOR**: Novos recursos mantendo retrocompatibilidade
- **PATCH**: Correções de bugs mantendo retrocompatibilidade
- **PRERELEASE**: Versões de teste (alpha, beta, rc)

## Boas Práticas

### Antes de Publicar

1. ✅ Todos os testes devem passar
2. ✅ O build deve ser bem-sucedido
3. ✅ A documentação deve estar atualizada
4. ✅ O CHANGELOG deve incluir as mudanças
5. ✅ A versão deve seguir o Semantic Versioning

### Checklist de Release

- [ ] Atualizar CHANGELOG.md com as mudanças
- [ ] Atualizar versão em `SbeSourceGenerator.csproj` (se necessário)
- [ ] Fazer merge de todas as PRs necessárias
- [ ] Executar testes localmente: `dotnet test`
- [ ] Criar tag e release no GitHub
- [ ] Verificar publicação no NuGet.org
- [ ] Validar que o pacote pode ser instalado

### Monitoramento

#### GitHub Actions

- Verifique regularmente a aba **Actions** para garantir que os workflows estão executando corretamente
- Configure notificações para falhas de workflow

#### NuGet.org

- Monitore as estatísticas de download em [NuGet.org](https://www.nuget.org/)
- Verifique se há problemas reportados na página do pacote

## Troubleshooting

### Erro: "Package already exists"

**Causa**: Tentativa de publicar uma versão que já existe no NuGet.org

**Solução**: 
- Use uma nova versão incrementada
- O flag `--skip-duplicate` no workflow previne que isso cause falha

### Erro: "Authentication failed"

**Causa**: API Key inválida ou expirada

**Solução**:
1. Gere uma nova API Key no NuGet.org
2. Atualize o secret `NUGET_API_KEY` no GitHub

### Erro: "Build failed"

**Causa**: Problemas de compilação ou dependências

**Solução**:
1. Execute `dotnet build` localmente
2. Resolva os erros de compilação
3. Faça commit das correções
4. O workflow será executado novamente

### Erro: "Tests failed"

**Causa**: Testes falhando

**Solução**:
1. Execute `dotnet test` localmente
2. Corrija os testes falhando
3. Faça commit das correções
4. Aguarde nova execução do workflow

## Status e Badges

Adicione badges no README.md para mostrar o status dos workflows:

```markdown
[![CI](https://github.com/pedrosakuma/SbeSourceGenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/pedrosakuma/SbeSourceGenerator/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/SbeSourceGenerator.svg)](https://www.nuget.org/packages/SbeSourceGenerator/)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SbeSourceGenerator.svg)](https://www.nuget.org/packages/SbeSourceGenerator/)
```

## Arquitetura da Pipeline

```
┌─────────────────────────────────────────────────────────────┐
│                         Desenvolvedor                        │
└────────────────┬────────────────────────────────────────────┘
                 │
                 ├─── Push/PR ────────────────────┐
                 │                                 │
                 ├─── Create Release ──────┐      │
                 │                         │      │
                 v                         v      v
┌────────────────────────────┐   ┌──────────────────────────┐
│      CI Workflow           │   │    CD Workflow           │
├────────────────────────────┤   ├──────────────────────────┤
│ 1. Checkout                │   │ 1. Checkout              │
│ 2. Setup .NET              │   │ 2. Setup .NET            │
│ 3. Restore                 │   │ 3. Extract Version       │
│ 4. Build                   │   │ 4. Restore               │
│ 5. Test                    │   │ 5. Build (versioned)     │
│ 6. Pack (dry-run)          │   │ 6. Test                  │
│ 7. Upload Artifacts        │   │ 7. Pack (versioned)      │
└────────────┬───────────────┘   │ 8. Push to NuGet.org     │
             │                   │ 9. Upload Artifacts      │
             v                   └────────────┬─────────────┘
   ✅ Build Status                            │
   ✅ Test Results                            v
   📦 Artifacts (7 days)              ✅ Published to NuGet
                                      📦 Artifacts (90 days)
```

## Recursos Adicionais

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Publishing NuGet packages](https://docs.microsoft.com/en-us/nuget/nuget-org/publish-a-package)
- [Semantic Versioning](https://semver.org/)
- [.NET CLI Reference](https://docs.microsoft.com/en-us/dotnet/core/tools/)

## Contato e Suporte

Para questões sobre a pipeline de CI/CD:
- Abra uma issue no GitHub
- Consulte a documentação do projeto
- Verifique os logs de execução no GitHub Actions

---

**Última Atualização**: 2025-10-13  
**Versão do Documento**: 1.0
