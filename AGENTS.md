# AGENTS.md - Contexto do Projeto para Agentes de IA

## O que é esse projeto

Aplicativo pessoal de finanças usado pelo casal para registrar gastos mensais e dividi-los proporcionalmente ao salário de cada um (quem ganha mais paga mais).

É um monorepo com frontend Vue.js e backend .NET, consumindo um banco PostgreSQL remoto.

---

## Estrutura do repositório

```
financas/
+-- frontend/    # Vue 3 + TypeScript (PWA)
+-- backend/     # ASP.NET Core 9 + PostgreSQL
```

---

## Frontend (`/frontend`)

**Stack:** Vue 3, TypeScript, Vite, Pinia, Naive UI, Axios, Clerk (auth)

**Como rodar:**
```bash
cd frontend
npm install
npm run dev     # http://localhost:5173
```

**Estrutura relevante:**
```
src/
├── api/           # Chamadas HTTP ao backend (axios) -um arquivo por domínio
├── components/    # Componentes reutilizáveis
├── views/         # Páginas (mapeadas no router)
├── stores/        # Pinia: user, currentMonth, currentYear
├── types/         # Interfaces TypeScript de todos os domínios
├── interceptor/   # Axios interceptor -injeta Bearer token do Clerk
└── router/        # Vue Router
```

**Rotas:**

- `/` -Home (lista de gastos do mês)
- `/addExpense` -Adicionar gasto
- `/import` -Importar CSV
- `/groups` -Gerenciar grupos

**Autenticação:** Clerk. O token JWT é injetado automaticamente em todas as requests via `src/interceptor/axios.ts`.

**Estado global (Pinia):**

- `useUserStore` -dados do usuário logado
- `useMonthStore` -mês selecionado (1–12)
- `useYearStore` -ano selecionado

**Variáveis de ambiente (`.env`):**

- `VITE_BACKEND_URL` -URL base do backend (ex: `https://localhost:7114`)
- `VITE_CLERK_PUBLISHABLE_KEY` -chave pública do Clerk

**Observação:** Ainda há código Firebase em `src/firebase/` sendo eliminado gradualmente. Não adicionar novas dependências do Firebase.

---

## Backend (`/backend`)

**Stack:** ASP.NET Core 9, Entity Framework Core, PostgreSQL (Npgsql), Clerk (auth)

**Como rodar:**
```bash
cd backend
dotnet run     # https://localhost:7114
```

**Estrutura:**
```
Auth/          # ClerkAuthorizeAttribute -valida JWT do Clerk em cada endpoint
Controller/    # Controllers REST (sem camada de serviço -acesso direto ao DbContext)
Database/      # FinanceContext (EF Core DbContext)
Dto/           # DTOs de entrada e saída
Migrations/    # Migrations do EF Core
Model/         # Entidades do banco
```

**Modelos principais:**

- `User` -usuário (Id string, Name, Email, Photo)
- `Expense` -gasto (Description, Value, Date, CategoryId, UserId)
- `ExpenseCategory` -categoria (Name, UserId)
- `Group` -grupo de divisão (Name, UserId dono)
- `GroupMember` -membro de grupo (GroupId + UserId, IsActive)

**Endpoints:**

| Controller | Método | Rota |
|---|---|---|
| Users | GET | `/api/users` -usuário atual |
| Users | GET | `/api/users/{id}` |
| Users | POST | `/api/users` |
| Users | PUT | `/api/users/{id}` |
| Users | DELETE | `/api/users/{id}` |
| Expenses | GET | `/api/users/{userId}/expenses` |
| Expenses | GET | `/api/users/{userId}/expenses/by-range?startDate=&endDate=` |
| Expenses | POST | `/api/expenses` |
| Expenses | POST | `/api/expenses/PostBulkExpense` |
| Expenses | DELETE | `/api/expenses/{expenseId}` |
| ExpenseCategory | GET | `/api/users/{userId}/expensecategory` |
| ExpenseCategory | POST | `/api/expensecategory` |
| GroupMember | GET | `/api/users/{userId}/groupmember` |
| GroupMember | GET | `/api/users/{userId}/groupmember/invites` |
| GroupMember | GET | `/api/groups/{groupId}/groupmember` |
| GroupMember | POST | `/api/groupmember` -criar grupo |
| GroupMember | POST | `/api/groupmember/NewMember` -convidar por email |
| GroupMember | PUT | `/api/groupmember/Accept` |
| GroupMember | DELETE | `/api/groupmember/Refuse` |
| GroupMember | DELETE | `/api/groupmember/Member` |
| GroupMember | DELETE | `/api/groupmember/Group` |

**Autenticação:** Todo endpoint usa `[ClerkAuthorize]`. O atributo valida o Bearer token e extrai o userId do claim `sub`.

**Configuração (`appsettings.json`):**

- `ConnectionStrings:AppDbContext` -connection string PostgreSQL
- `frontendUrl` -origem permitida no CORS
- `clerkApiKey` -chave do Clerk

**Padrão arquitetural atual:** Controllers acessam `FinanceContext` diretamente (sem camada de serviço). Ao adicionar features, manter esse padrão por enquanto.

---

## Regras de negócio principais

- Cada usuário tem suas próprias categorias e gastos
- Grupos permitem que dois usuários compartilhem gastos do mês
- A divisão dos gastos é proporcional ao salário de cada membro (quem ganha mais paga mais)
- Convites para grupos são feitos por email; o convidado precisa aceitar

---

## O que está em andamento / incompleto

- Remoção do Firebase (ainda há código em `frontend/src/firebase/`)
- A lógica de cálculo proporcional por salário ainda não está implementada no backend
- Tipos Firebase ainda presentes em `frontend/src/types/index.ts` (MonthGroup, PersonalInformation, CollaboratorResult)

---

## Convenções

- Frontend em português brasileiro na UI, código em inglês
- Backend: nomes de classes/métodos em inglês, C# padrão PascalCase
- Commits descritivos no padrão `tipo: descrição` (feat, fix, chore, refactor)
- Não usar Firebase para nada novo
