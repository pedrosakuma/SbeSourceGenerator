# Remover Arquivo Sensível do Histórico do Git / Remove Sensitive File from Git History

## 🔒 Problema de Segurança / Security Issue

O arquivo `examples/SbeBinanceConsole/Properties/launchSettings.json` contém secrets (credenciais) vazadas e precisa ser completamente removido do histórico do Git.

The file `examples/SbeBinanceConsole/Properties/launchSettings.json` contains leaked secrets (credentials) and needs to be completely removed from Git history.

## ✅ Status Atual / Current Status

- ✅ O arquivo foi removido do working tree no commit `80a8edb47f62c1cceb5ec8814bb8db643020282f`
- ✅ O `.gitignore` foi atualizado para ignorar `**/launchSettings.json` (linha 373)
- ❌ O arquivo ainda existe no histórico do Git (commit `b97aba181660ba29d5c5b2353424195c14a69633` e outros)

- ✅ The file was removed from the working tree in commit `80a8edb47f62c1cceb5ec8814bb8db643020282f`
- ✅ The `.gitignore` was updated to ignore `**/launchSettings.json` (line 373)
- ❌ The file still exists in Git history (commit `b97aba181660ba29d5c5b2353424195c14a69633` and others)

## 🚨 Ações Imediatas Necessárias / Immediate Actions Required

### 1. Rotacionar Secrets Comprometidos / Rotate Compromised Secrets

**ANTES de remover o histórico, você DEVE rotacionar todos os secrets expostos:**

**BEFORE removing the history, you MUST rotate all exposed secrets:**

- [ ] Binance API Keys
- [ ] Quaisquer outros tokens ou credenciais que estavam no arquivo

Qualquer pessoa com acesso ao repositório pode ter copiado esses secrets.

Anyone with access to the repository may have copied these secrets.

### 2. Verificar GitHub Secret Scanning

GitHub pode ter detectado automaticamente os secrets:

1. Visite: https://github.com/pedrosakuma/SbeSourceGenerator/security/secret-scanning
2. Revise todos os alertas
3. Revogue e regenere todos os secrets detectados

GitHub may have automatically detected the secrets:

1. Visit: https://github.com/pedrosakuma/SbeSourceGenerator/security/secret-scanning
2. Review all alerts
3. Revoke and regenerate all detected secrets

## 🛠️ Opções para Remover do Histórico / Options to Remove from History

### Opção 1: BFG Repo Cleaner (Recomendado / Recommended)

O BFG é mais rápido e mais fácil que git-filter-branch.

BFG is faster and easier than git-filter-branch.

```bash
# 1. Fazer backup do repositório
git clone --mirror https://github.com/pedrosakuma/SbeSourceGenerator.git

# 2. Baixar BFG
# https://rtyley.github.io/bfg-repo-cleaner/
# wget https://repo1.maven.org/maven2/com/madgag/bfg/1.14.0/bfg-1.14.0.jar

# 3. Remover o arquivo do histórico
java -jar bfg-1.14.0.jar --delete-files launchSettings.json SbeSourceGenerator.git

# 4. Limpar e compactar o repositório
cd SbeSourceGenerator.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 5. Force push para o remote (CUIDADO!)
git push --force
```

### Opção 2: git filter-repo (Moderno / Modern)

```bash
# 1. Instalar git-filter-repo
# pip install git-filter-repo

# 2. Clonar o repositório
git clone https://github.com/pedrosakuma/SbeSourceGenerator.git
cd SbeSourceGenerator

# 3. Remover o arquivo
git filter-repo --path examples/SbeBinanceConsole/Properties/launchSettings.json --invert-paths

# 4. Force push para o remote
git remote add origin https://github.com/pedrosakuma/SbeSourceGenerator.git
git push --force --all
git push --force --tags
```

### Opção 3: git filter-branch (Legado / Legacy)

```bash
# 1. Clonar o repositório
git clone https://github.com/pedrosakuma/SbeSourceGenerator.git
cd SbeSourceGenerator

# 2. Remover o arquivo do histórico
git filter-branch --force --index-filter \
  'git rm --cached --ignore-unmatch examples/SbeBinanceConsole/Properties/launchSettings.json' \
  --prune-empty --tag-name-filter cat -- --all

# 3. Limpar referências
git for-each-ref --format='delete %(refname)' refs/original | git update-ref --stdin
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# 4. Force push para o remote
git push --force --all
git push --force --tags
```

### Opção 4: GitHub Support (Se você não puder fazer force push)

Se você não tiver permissões para force push ou preferir assistência:

If you don't have permissions for force push or prefer assistance:

1. Abra um ticket no GitHub Support: https://support.github.com/
2. Selecione "Repository security" como categoria
3. Explique que você precisa remover secrets expostos do histórico
4. Forneça o caminho do arquivo: `examples/SbeBinanceConsole/Properties/launchSettings.json`
5. Forneça os commits: `b97aba181660ba29d5c5b2353424195c14a69633` e `80a8edb47f62c1cceb5ec8814bb8db643020282f`

## ⚠️ Avisos Importantes / Important Warnings

### Antes de Reescrever o Histórico / Before Rewriting History

1. **Notifique todos os colaboradores** - Eles precisarão re-clonar o repositório
2. **Faça backup completo** - Mantenha um backup local antes de começar
3. **Revogue os secrets** - Secrets antigos devem ser considerados comprometidos
4. **Considere o impacto** - PRs abertos e branches precisarão ser recriados

1. **Notify all collaborators** - They will need to re-clone the repository
2. **Full backup** - Keep a local backup before starting
3. **Revoke secrets** - Old secrets should be considered compromised
4. **Consider the impact** - Open PRs and branches will need to be recreated

### Após Reescrever o Histórico / After Rewriting History

Todos os colaboradores devem executar:

All collaborators should run:

```bash
# DELETAR o clone local antigo
rm -rf SbeSourceGenerator

# Re-clonar do repositório limpo
git clone https://github.com/pedrosakuma/SbeSourceGenerator.git
```

## 📋 Checklist de Segurança / Security Checklist

- [ ] Secrets foram rotacionados/revogados
- [ ] Novos secrets foram gerados
- [ ] Verificar GitHub Secret Scanning
- [ ] Notificar colaboradores
- [ ] Fazer backup do repositório
- [ ] Executar BFG/git-filter-repo/git-filter-branch
- [ ] Force push para o remote
- [ ] Verificar que o arquivo foi removido do histórico
- [ ] Todos os colaboradores re-clonaram o repositório
- [ ] Atualizar documentação sobre o incidente

- [ ] Secrets were rotated/revoked
- [ ] New secrets were generated
- [ ] Check GitHub Secret Scanning
- [ ] Notify collaborators
- [ ] Backup repository
- [ ] Run BFG/git-filter-repo/git-filter-branch
- [ ] Force push to remote
- [ ] Verify file was removed from history
- [ ] All collaborators re-cloned the repository
- [ ] Update documentation about the incident

## 🔍 Verificação / Verification

Após remover o arquivo do histórico, verifique:

After removing the file from history, verify:

```bash
# Procurar o arquivo em todo o histórico
git log --all --full-history -- "**/launchSettings.json"

# Deve retornar vazio se foi removido com sucesso
# Should return empty if successfully removed

# Verificar o conteúdo de todos os commits
git rev-list --all | while read commit; do
  if git ls-tree -r $commit | grep -q "launchSettings.json"; then
    echo "Found in commit: $commit"
  fi
done
```

## 📚 Recursos Adicionais / Additional Resources

- [BFG Repo Cleaner](https://rtyley.github.io/bfg-repo-cleaner/)
- [git-filter-repo](https://github.com/newren/git-filter-repo)
- [GitHub: Removing sensitive data](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository)
- [GitHub Secret Scanning](https://docs.github.com/en/code-security/secret-scanning/about-secret-scanning)

## 🆘 Precisa de Ajuda? / Need Help?

Se você tiver dúvidas ou problemas:

If you have questions or problems:

1. Revise a documentação do GitHub sobre remoção de dados sensíveis
2. Abra um issue no repositório (SEM incluir os secrets!)
3. Contate GitHub Support para assistência

1. Review GitHub documentation about removing sensitive data
2. Open an issue in the repository (WITHOUT including the secrets!)
3. Contact GitHub Support for assistance

---

**Data da Criação / Creation Date:** 2025-10-31  
**Última Atualização / Last Updated:** 2025-10-31
