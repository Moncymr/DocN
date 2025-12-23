#!/bin/bash

# =============================================
# Script: run_setup.sh
# Description: Script Bash per eseguire il setup del database DocN
# =============================================

set -e

# Colori per output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# Funzione di aiuto
show_help() {
    echo -e "${CYAN}========================================${NC}"
    echo -e "${CYAN}Setup Database DocN${NC}"
    echo -e "${CYAN}========================================${NC}"
    echo ""
    echo "Uso: $0 -s SERVER -d DATABASE [-u USERNAME] [-p PASSWORD] [-w]"
    echo ""
    echo "Opzioni:"
    echo "  -s SERVER      Nome del server SQL"
    echo "  -d DATABASE    Nome del database"
    echo "  -u USERNAME    Username SQL (opzionale, richiesto senza -w)"
    echo "  -p PASSWORD    Password SQL (opzionale, richiesto senza -w)"
    echo "  -w             Usa Windows Authentication (solo Windows)"
    echo "  -h             Mostra questo messaggio di aiuto"
    echo ""
    echo "Esempi:"
    echo "  # SQL Server Authentication:"
    echo "  $0 -s localhost -d DocN -u sa -p 'YourPassword'"
    echo ""
    echo "  # Windows Authentication (solo Windows):"
    echo "  $0 -s localhost -d DocN -w"
    echo ""
    exit 0
}

# Variabili
SERVER=""
DATABASE=""
USERNAME=""
PASSWORD=""
USE_WIN_AUTH=false

# Parse argomenti
while getopts "s:d:u:p:wh" opt; do
    case $opt in
        s) SERVER="$OPTARG" ;;
        d) DATABASE="$OPTARG" ;;
        u) USERNAME="$OPTARG" ;;
        p) PASSWORD="$OPTARG" ;;
        w) USE_WIN_AUTH=true ;;
        h) show_help ;;
        *) show_help ;;
    esac
done

# Verifica parametri obbligatori
if [ -z "$SERVER" ] || [ -z "$DATABASE" ]; then
    echo -e "${RED}ERRORE: SERVER e DATABASE sono obbligatori${NC}"
    echo ""
    show_help
fi

# Verifica credenziali
if [ "$USE_WIN_AUTH" = false ]; then
    if [ -z "$USERNAME" ] || [ -z "$PASSWORD" ]; then
        echo -e "${RED}ERRORE: USERNAME e PASSWORD sono richiesti per l'autenticazione SQL${NC}"
        echo "Oppure usa -w per Windows Authentication"
        exit 1
    fi
fi

# Verifica che sqlcmd sia installato
if ! command -v sqlcmd &> /dev/null; then
    echo -e "${RED}ERRORE: sqlcmd non è installato${NC}"
    echo "Installare SQL Server Command Line Tools"
    echo "Vedere: https://learn.microsoft.com/en-us/sql/tools/sqlcmd-utility"
    exit 1
fi

echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Setup Database DocN${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""
echo -e "${YELLOW}Server: $SERVER${NC}"
echo -e "${YELLOW}Database: $DATABASE${NC}"

if [ "$USE_WIN_AUTH" = true ]; then
    echo -e "${GREEN}Autenticazione: Windows${NC}"
else
    echo -e "${GREEN}Autenticazione: SQL Server${NC}"
fi

echo ""
echo -e "${CYAN}========================================${NC}"

# Array degli script
SCRIPTS=(
    "01_CreateIdentityTables.sql"
    "02_CreateDocumentTables.sql"
    "03_ConfigureFullTextSearch.sql"
)

# Directory dello script
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"

# Esegui ogni script
SUCCESS=true
for SCRIPT in "${SCRIPTS[@]}"; do
    SCRIPT_PATH="$SCRIPT_DIR/$SCRIPT"
    
    if [ ! -f "$SCRIPT_PATH" ]; then
        echo -e "${RED}ERRORE: Script $SCRIPT non trovato in $SCRIPT_DIR${NC}"
        SUCCESS=false
        break
    fi
    
    echo ""
    echo -e "${YELLOW}Esecuzione: $SCRIPT${NC}"
    echo "----------------------------------------"
    
    if [ "$USE_WIN_AUTH" = true ]; then
        sqlcmd -S "$SERVER" -d "$DATABASE" -E -i "$SCRIPT_PATH" -b
    else
        sqlcmd -S "$SERVER" -d "$DATABASE" -U "$USERNAME" -P "$PASSWORD" -i "$SCRIPT_PATH" -b
    fi
    
    if [ $? -eq 0 ]; then
        echo -e "${GREEN}✓ $SCRIPT completato con successo${NC}"
    else
        echo -e "${RED}✗ $SCRIPT fallito${NC}"
        SUCCESS=false
        break
    fi
done

echo ""
echo -e "${CYAN}========================================${NC}"

if [ "$SUCCESS" = true ]; then
    echo -e "${GREEN}✅ Setup Database DocN completato con successo!${NC}"
else
    echo -e "${RED}❌ Setup Database DocN fallito. Verificare gli errori sopra.${NC}"
    exit 1
fi

echo -e "${CYAN}========================================${NC}"
echo ""
