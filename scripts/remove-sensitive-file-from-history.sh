#!/bin/bash
# Script para remover launchSettings.json do histórico do Git usando BFG Repo Cleaner
# Script to remove launchSettings.json from Git history using BFG Repo Cleaner

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${RED}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${RED}║  AVISO: Este script reescreve o histórico do Git!             ║${NC}"
echo -e "${RED}║  WARNING: This script rewrites Git history!                   ║${NC}"
echo -e "${RED}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${YELLOW}ANTES de continuar, certifique-se de que:${NC}"
echo -e "${YELLOW}BEFORE continuing, make sure that:${NC}"
echo "1. ✅ Você REVOGOU todas as API keys expostas"
echo "   ✅ You REVOKED all exposed API keys"
echo "2. ✅ Você notificou todos os colaboradores"
echo "   ✅ You notified all collaborators"
echo "3. ✅ Você fez backup do repositório"
echo "   ✅ You backed up the repository"
echo ""
read -p "Você confirma que completou todas as etapas acima? (yes/no): " -r
echo ""
if [[ ! $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${RED}Operação cancelada. Complete as etapas acima primeiro!${NC}"
    echo -e "${RED}Operation cancelled. Complete the steps above first!${NC}"
    exit 1
fi

# Configuration
REPO_URL="https://github.com/pedrosakuma/SbeSourceGenerator.git"
REPO_NAME="SbeSourceGenerator"
BFG_VERSION="1.14.0"
BFG_JAR="bfg-${BFG_VERSION}.jar"
BFG_URL="https://repo1.maven.org/maven2/com/madgag/bfg/${BFG_VERSION}/${BFG_JAR}"
WORK_DIR="git-history-cleanup"
FILE_TO_REMOVE="launchSettings.json"

echo -e "${GREEN}Step 1: Criando diretório de trabalho / Creating work directory${NC}"
mkdir -p "$WORK_DIR"
cd "$WORK_DIR"

# Download BFG if not exists
if [ ! -f "$BFG_JAR" ]; then
    echo -e "${GREEN}Step 2: Baixando BFG Repo Cleaner / Downloading BFG Repo Cleaner${NC}"
    wget "$BFG_URL" -O "$BFG_JAR"
else
    echo -e "${YELLOW}BFG já existe, pulando download / BFG already exists, skipping download${NC}"
fi

# Clone mirror
if [ ! -d "${REPO_NAME}.git" ]; then
    echo -e "${GREEN}Step 3: Clonando repositório (mirror) / Cloning repository (mirror)${NC}"
    git clone --mirror "$REPO_URL"
else
    echo -e "${YELLOW}Repositório mirror já existe / Mirror repository already exists${NC}"
    cd "${REPO_NAME}.git"
    echo -e "${GREEN}Atualizando mirror / Updating mirror${NC}"
    git fetch --all
    cd ..
fi

# Run BFG
echo -e "${GREEN}Step 4: Removendo arquivo do histórico / Removing file from history${NC}"
java -jar "$BFG_JAR" --delete-files "$FILE_TO_REMOVE" "${REPO_NAME}.git"

# Cleanup
echo -e "${GREEN}Step 5: Limpando repositório / Cleaning up repository${NC}"
cd "${REPO_NAME}.git"
git reflog expire --expire=now --all
git gc --prune=now --aggressive

# Verification
echo ""
echo -e "${GREEN}Verificação / Verification:${NC}"
echo "Procurando $FILE_TO_REMOVE no histórico..."
echo "Searching for $FILE_TO_REMOVE in history..."

if git log --all --full-history -- "**/$FILE_TO_REMOVE" | grep -q commit; then
    echo -e "${RED}❌ ERRO: O arquivo ainda existe no histórico!${NC}"
    echo -e "${RED}❌ ERROR: The file still exists in history!${NC}"
    exit 1
else
    echo -e "${GREEN}✅ Sucesso! O arquivo foi removido do histórico.${NC}"
    echo -e "${GREEN}✅ Success! The file was removed from history.${NC}"
fi

echo ""
echo -e "${YELLOW}╔════════════════════════════════════════════════════════════════╗${NC}"
echo -e "${YELLOW}║  PRÓXIMO PASSO CRÍTICO / NEXT CRITICAL STEP                    ║${NC}"
echo -e "${YELLOW}╚════════════════════════════════════════════════════════════════╝${NC}"
echo ""
echo -e "${RED}Para aplicar as mudanças ao repositório remoto, execute:${NC}"
echo -e "${RED}To apply changes to the remote repository, run:${NC}"
echo ""
echo -e "${GREEN}    git push --force${NC}"
echo ""
echo -e "${YELLOW}AVISO: Após o force push, todos os colaboradores devem:${NC}"
echo -e "${YELLOW}WARNING: After force push, all collaborators must:${NC}"
echo "1. Deletar seus clones locais / Delete their local clones"
echo "2. Re-clonar o repositório / Re-clone the repository"
echo ""
echo -e "${YELLOW}Executar force push agora? (yes/no): ${NC}"
read -p "" -r
echo ""
if [[ $REPLY =~ ^[Yy][Ee][Ss]$ ]]; then
    echo -e "${GREEN}Executando force push... / Executing force push...${NC}"
    git push --force
    echo ""
    echo -e "${GREEN}✅ CONCLUÍDO! Histórico limpo e enviado ao remote.${NC}"
    echo -e "${GREEN}✅ COMPLETE! History cleaned and pushed to remote.${NC}"
    echo ""
    echo -e "${YELLOW}⚠️  NOTIFIQUE todos os colaboradores para re-clonar!${NC}"
    echo -e "${YELLOW}⚠️  NOTIFY all collaborators to re-clone!${NC}"
else
    echo -e "${YELLOW}Force push cancelado. Você pode executá-lo manualmente mais tarde:${NC}"
    echo -e "${YELLOW}Force push cancelled. You can run it manually later:${NC}"
    echo ""
    echo "    cd $(pwd)"
    echo "    git push --force"
fi

cd ../..

echo ""
echo -e "${GREEN}Diretório de trabalho: $(pwd)/$WORK_DIR${NC}"
echo -e "${GREEN}Working directory: $(pwd)/$WORK_DIR${NC}"
