# 🚨 INCIDENTE DE SEGURANÇA - AÇÃO IMEDIATA NECESSÁRIA

## Resumo do Problema

O arquivo `examples/SbeBinanceConsole/Properties/launchSettings.json` contém **API keys do Binance** vazadas no histórico do Git.

**Commits afetados:**
- `b97aba181660ba29d5c5b2353424195c14a69633` - Arquivo adicionado/modificado (28 Oct 2025)
- `80a8edb47f62c1cceb5ec8814bb8db643020282f` - Arquivo removido (29 Oct 2025)

## ⚡ AÇÕES IMEDIATAS (FAÇA AGORA!)

### 1. REVOGAR API KEYS COMPROMETIDAS ⏰

**CRÍTICO:** Vá agora para o painel do Binance e:

1. Acesse: https://www.binance.com/en/my/settings/api-management (URL verificada em 2025-10-31)
2. **REVOGUE** todas as API keys que estavam no arquivo
3. **DELETE** as API keys antigas
4. Gere novas API keys se necessário
5. **NÃO** commit as novas keys no Git!

### 2. VERIFICAR GitHub Secret Scanning

1. Visite: https://github.com/pedrosakuma/SbeSourceGenerator/security/secret-scanning
2. Se houver alertas, siga as instruções do GitHub

### 3. REMOVER DO HISTÓRICO GIT

**Método Recomendado: BFG Repo Cleaner**

```bash
# Download BFG
wget https://repo1.maven.org/maven2/com/madgag/bfg/1.14.0/bfg-1.14.0.jar

# Clone mirror
git clone --mirror https://github.com/pedrosakuma/SbeSourceGenerator.git

# Remove file from history
java -jar bfg-1.14.0.jar --delete-files launchSettings.json SbeSourceGenerator.git

# Clean up
cd SbeSourceGenerator.git
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Force push (AVISO: reescreve histórico!)
git push --force
```

**IMPORTANTE:** Depois do force push, todos os colaboradores precisam deletar e re-clonar o repositório!

## 📋 Checklist

- [ ] ✅ API keys do Binance foram REVOGADAS
- [ ] ✅ Novas API keys geradas (se necessário)
- [ ] ✅ GitHub Secret Scanning verificado
- [ ] ✅ Histórico do Git limpo com BFG
- [ ] ✅ Force push executado
- [ ] ✅ Colaboradores notificados para re-clonar
- [ ] ✅ Verificado que arquivo não está mais no histórico

## 🔍 Verificação Final

```bash
# Verificar se arquivo foi removido do histórico
git log --all --full-history -- "**/launchSettings.json"
# Deve retornar vazio!
```

## 📞 Precisa de Ajuda?

- Documentação completa: `docs/REMOVE_SENSITIVE_FILE_FROM_HISTORY.md`
- GitHub Support: https://support.github.com/
- BFG Docs: https://rtyley.github.io/bfg-repo-cleaner/

---

**⏰ TEMPO É CRÍTICO:** Quanto mais tempo as API keys permanecerem ativas, maior o risco de comprometimento!
