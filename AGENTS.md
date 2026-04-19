# AGENTS.md - Contexto do Projeto para Agentes de IA

## O que é esse projeto

Aplicativo pessoal de finanças usado pelo casal para registrar gastos mensais e dividi-los proporcionalmente ao salário de cada um (quem ganha mais paga mais).

É um monorepo com frontend Vue.js e backend .NET, consumindo um banco PostgreSQL remoto.

---

## Estrutura do repositório

```text
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

```text
src/
├── api/           # Chamadas HTTP ao backend (axios) - um arquivo por domínio
├── components/    # Componentes reutilizáveis
├── views/         # Páginas (mapeadas no router)
├── stores/        # Pinia: user, currentMonth, currentYear
├── types/         # Interfaces TypeScript de todos os domínios
├── interceptor/   # Axios interceptor - injeta Bearer token do Clerk
└── router/        # Vue Router
```

**Rotas:**

- `/` - Home (lista de gastos do mês)
- `/addExpense` - Adicionar gasto
- `/import` - Importar CSV
- `/groups` - Gerenciar grupos

**Autenticação:** Clerk. O token JWT é injetado automaticamente em todas as requests via `src/interceptor/axios.ts`.

**Estado global (Pinia):**

- `useUserStore` - dados do usuário logado
- `useMonthStore` - mês selecionado (1–12)
- `useYearStore` - ano selecionado
- `useSelectedGroupStore` - grupo padrão selecionado no navbar (usado pela HomeView para exibir despesas e resumo de divisão do grupo)

**Variáveis de ambiente (`.env`):**

- `VITE_BACKEND_URL` - URL base do backend (ex: `https://localhost:7114`)
- `VITE_CLERK_PUBLISHABLE_KEY` - chave pública do Clerk

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

```text
Auth/          # ClerkAuthorizeAttribute - valida JWT do Clerk em cada endpoint
Controller/    # Controllers REST finos - delegam para Services/
Database/      # FinanceContext (EF Core DbContext)
Dto/           # DTOs de entrada e saída
Migrations/    # Migrations do EF Core
Model/         # Entidades do banco
Services/      # Lógica de negócio (IExpenseService, IGroupService, IExpenseCategoryService)
```

**Modelos principais:**

- `User` - usuário (Id string, Name, Email, Photo)
- `Expense` - gasto (Description, Value, Date, CategoryId, UserId, GroupId?)
- `ExpenseCategory` - categoria (Name, UserId)
- `ExpenseSplitConfig` - qual `GroupSplitConfig` se aplica a uma despesa específica (ExpenseId, GroupSplitConfigId)
- `Group` - grupo de divisão (Name, UserId dono)
- `GroupMember` - membro de grupo (GroupId + UserId, IsActive)
- `GroupMemberConfig` - config do membro no grupo (GroupId, UserId, Salary?) — FK em Group + User (não em GroupMember, para suportar o dono)
- `GroupSplitConfig` - configuração de divisão do grupo (GroupId, SplitType: Equal/Proportional/Manual, IsDefault)
- `GroupSplitConfigShare` - percentual manual por membro (GroupSplitConfigId, UserId, Percentage)
- `GroupMonthClose` - registro de fechamento de mês (GroupId, Month, Year, ClosedAt?)
- `GroupMonthCloseConfirmation` - confirmação individual de membro (GroupMonthCloseId, UserId, ConfirmedAt)
- `ExpenseSplitShare` - snapshot congelado da divisão ao fechar o mês (ExpenseId, UserId, Percentage, Amount)

**Endpoints:**

| Controller | Método | Rota |
| --- | --- | --- |
| Users | GET | `/api/users/{id}` |
| Users | POST | `/api/users` |
| Users | PUT | `/api/users/{id}` |
| Users | DELETE | `/api/users/{id}` |
| Expenses | GET | `/api/users/{userId}/expenses` |
| Expenses | GET | `/api/users/{userId}/expenses/by-range?startDate=&endDate=` |
| Expenses | POST | `/api/expenses` — cria despesa (aceita `groupId?` e `splitType?`) |
| Expenses | POST | `/api/expenses/bulk` |
| Expenses | PATCH | `/api/expenses/{id}/group` — vincula/desvincula grupo (body: `groupId?`, `splitType?`) |
| Expenses | DELETE | `/api/expenses/{expenseId}` |
| ExpenseCategory | GET | `/api/users/{userId}/expensecategory` |
| ExpenseCategory | POST | `/api/expensecategory` |
| Groups | GET | `/api/users/{userId}/groups` |
| Groups | GET | `/api/users/{userId}/group-invitations` |
| Groups | GET | `/api/groups/{groupId}/members` |
| Groups | GET | `/api/groups/{groupId}/expenses?startDate=&endDate=` |
| Groups | POST | `/api/groups` — criar grupo |
| Groups | POST | `/api/groups/{groupId}/members` — convidar por email |
| Groups | PUT | `/api/groups/{groupId}/invitations/{userId}/accept` |
| Groups | DELETE | `/api/groups/{groupId}` |
| Groups | DELETE | `/api/groups/{groupId}/members/{userId}` |
| Groups | DELETE | `/api/groups/{groupId}/invitations/{userId}` |
| Groups | PUT | `/api/groups/{groupId}/members/{userId}/salary` — define salário do membro |
| GroupSplitConfig | GET | `/api/groups/{groupId}/split-configs` |
| GroupSplitConfig | POST | `/api/groups/{groupId}/split-configs` |
| GroupSplitConfig | PUT | `/api/groups/{groupId}/split-configs/{configId}` |
| GroupSplitConfig | DELETE | `/api/groups/{groupId}/split-configs/{configId}` |
| GroupSplitSummary | GET | `/api/groups/{groupId}/split-summary?month=&year=` — calcula (ou retorna congelado) a divisão do mês |
| GroupSplitSummary | GET | `/api/groups/{groupId}/month-close/pending` — meses anteriores ainda abertos |
| GroupSplitSummary | GET | `/api/groups/{groupId}/month-close/{month}/{year}` — status de fechamento do mês |
| GroupSplitSummary | POST | `/api/groups/{groupId}/month-close/{month}/{year}/confirm` — registra confirmação do membro |
| GroupSplitSummary | DELETE | `/api/groups/{groupId}/month-close/{month}/{year}/confirm` — remove confirmação |

**Autenticação:** Todo endpoint usa `[ClerkAuthorize]`. O atributo valida o Bearer token e extrai o userId do claim `sub`.

**Configuração (`appsettings.json`):**

- `ConnectionStrings:AppDbContext` - connection string PostgreSQL
- `frontendUrl` - origem permitida no CORS
- `clerkApiKey` - chave do Clerk

**Padrão arquitetural:** Controllers são finos — recebem a request, delegam para a camada de serviço (`Services/`) e mapeiam o `ServiceResult` para o status HTTP. Toda lógica de negócio e acesso ao banco fica nos serviços.

**Camada de serviços (`backend/Services/`):**

- `IUserService` / `UserService` — CRUD de usuários
- `IExpenseService` / `ExpenseService` — CRUD de despesas com verificações de autorização e bloqueio de mês fechado
- `IGroupService` / `GroupService` — gestão de grupos, convites, membros e salários
- `IExpenseCategoryService` / `ExpenseCategoryService` — CRUD de categorias
- `IGroupSplitConfigService` / `GroupSplitConfigService` — CRUD de configurações de divisão do grupo
- `ISplitCalculatorService` / `SplitCalculatorService` — calcula divisão ao vivo; se mês fechado, lê `ExpenseSplitShare` (resultado congelado)
- `IMonthCloseService` / `MonthCloseService` — gerencia confirmações de fechamento; ao fechar salva snapshot em `ExpenseSplitShare`

Os serviços retornam `ServiceResult<T>` ou `ServiceResult` (sem dados). O enum `ServiceError` possui: `None`, `NotFound`, `Unauthorized`, `Conflict`.

---

## Regras de negócio principais

- Cada usuário tem suas próprias categorias e gastos
- Grupos permitem que múltiplos usuários compartilhem gastos do mês
- Convites para grupos são feitos por email; o convidado precisa aceitar
- Cada grupo pode ter múltiplas configurações de divisão (`GroupSplitConfig`): Equal, Proportional ou Manual
- Uma config é marcada como padrão (`IsDefault`); cada despesa pode sobrescrever com uma config específica via `ExpenseSplitConfig`
- Para divisão proporcional, o salário de cada membro fica em `GroupMemberConfig` (cada membro edita o próprio salário)
- O resumo de divisão (`split-summary`) mostra: total gasto, quanto cada membro pagou, deveria pagar, saldo e direção (recebe/paga)
- **Fechamento de mês:** meses anteriores ao atual podem ser fechados. Todos os membros precisam confirmar. Ao fechar: salva snapshot em `ExpenseSplitShare` e bloqueia modificações
- **Mês fechado:** não é possível criar, deletar, desvincular ou alterar tipo de divisão de despesas de mês fechado. Retorna `409 Conflict`
- **Resultado congelado:** após fechar, `split-summary` retorna os valores do snapshot (`ExpenseSplitShare`), ignorando mudanças posteriores de salário ou config

---

## O que está em andamento / incompleto

- Remoção do Firebase (ainda há código em `frontend/src/firebase/`)
- Tipos Firebase ainda presentes em `frontend/src/types/index.ts` (MonthGroup, PersonalInformation, CollaboratorResult) — não utilizar, serão removidos

---

## Segurança — Credenciais

**Regra absoluta: nenhuma credencial jamais deve entrar no repositório**, nem em commits, nem em histórico.

- `backend/appsettings.json` — commitado com valores `"SEE_ENVIRONMENT"` como placeholder
- `backend/appsettings.Development.json` — **gitignored**, contém credenciais reais para dev local. Preencha com os valores reais após clonar.
- Em produção (Docker): passe as credenciais como variáveis de ambiente. O ASP.NET Core as lê automaticamente sobrepondo o `appsettings.json`:

```yaml
environment:
  - ConnectionStrings__AppDbContext=Host=...;Password=...
  - clerkApiKey=...
```

- Se suspeitar que uma credencial foi commitada: rotacione imediatamente antes de limpar o histórico.

---

## Testes

**Backend:** xUnit + EF InMemory. Projeto em `backend/MyFinBackend.Tests/`.

- Testes cobrem os serviços diretamente (não os controllers)
- Rodar com: `cd backend && dotnet test`

**Regras sobre testes:**

- Toda nova implementação de lógica de negócio deve incluir testes cobrindo os casos relevantes.
- Nunca altere um teste existente sem solicitação explícita do usuário. Se uma implementação quebra um teste, informe o motivo antes de alterar — ex: *"é necessário alterar o teste X por A e B razões"* — e aguarde confirmação. Testes que passam são contrato estabelecido.

---

## Pasta `temp/` — planejamentos locais

A pasta `temp/` na raiz do repositório é ignorada pelo git (ver `.gitignore`) e serve para guardar arquivos de planejamento, rascunhos e documentos temporários de desenvolvimento. Nada dentro dela deve ser commitado. Use-a livremente durante o desenvolvimento para organizar planos de implementação, anotações e estudos.

---

## Convenções

- **UI:** português brasileiro; código (variáveis, funções, classes) em inglês.
- **Backend:** C# padrão PascalCase para classes e métodos.
- **Commits:** seguir [Conventional Commits](https://www.conventionalcommits.org/) — `tipo(escopo): descrição`. Tipos comuns: `feat`, `fix`, `chore`, `refactor`, `docs`, `test`. Mensagem curta e objetiva, sem co-autoria automática.
- **Não usar Firebase** para nada novo.
- **Atualizar este arquivo** sempre que houver mudança relevante de arquitetura, novo padrão, nova regra de negócio ou novo domínio. O AGENTS.md é o contrato de contexto entre conversas — se não está aqui, o próximo agente não saberá.

---

## Padrões de qualidade obrigatórios

### Clean Code

- Nomes de classes, métodos, variáveis e parâmetros devem ser autoexplicativos (sem abreviações obscuras).
- Parâmetros de método em C# devem ser `camelCase` (não `PascalCase`).
- Nenhum código comentado deve permanecer no repositório — se não é usado, é removido.
- Sem `console.log` de debug no frontend — use o DevTools quando precisar inspecionar.
- Controllers devem ser finos: recebem a request, delegam ao service, mapeiam o `ServiceResult` para status HTTP. Nenhuma lógica de negócio ou acesso direto ao banco nos controllers.
- Credenciais e chaves nunca hardcoded — sempre via `IConfiguration`/`.env`.

### SOLID

- **SRP:** cada classe tem uma única responsabilidade. Controllers não acessam banco; services não conhecem HTTP.
- **OCP:** novos comportamentos adicionados via extensão (novos métodos/implementações), não modificando código existente que funciona.
- **LSP:** implementações de interface devem ser substituíveis pelo contrato sem surpresas.
- **ISP:** interfaces pequenas e coesas. Evitar interfaces gigantes que forçam implementações vazias.
- **DIP:** dependa de abstrações (`IExpenseService`, `IGroupService`, `IUserService`). Nunca instancie dependências concretas dentro de classes — injete pelo construtor.

### RESTful API

- **Recursos em substantivos plurais e minúsculos:** `/api/expenses`, `/api/groups`, `/api/users`.
- **Hierarquia de recursos:** sub-recursos como `/api/groups/{groupId}/members`, `/api/users/{userId}/groups`.
- **Verbos HTTP corretos:**
  - `GET` — leitura (nunca modifica estado)
  - `POST` — criação (retorna `201 Created` com o recurso criado e `Location` header)
  - `PUT` — substituição completa do recurso
  - `PATCH` — atualização parcial
  - `DELETE` — remoção (retorna `204 No Content`)
- **Sem verbos nas rotas:** `/api/expenses/bulk` ✅ — `/api/expenses/PostBulkExpense` ❌
- **Status HTTP semânticos:**
  - `200 OK` — leitura bem-sucedida ou atualização (PUT/PATCH) bem-sucedida
  - `201 Created` — criação bem-sucedida (POST)
  - `204 No Content` — deleção ou ação sem corpo de retorno
  - `400 Bad Request` — request mal-formada (body inválido, tipos errados)
  - `401 Unauthorized` — não autenticado (sem token ou token inválido)
  - `403 Forbidden` — autenticado mas sem permissão para o recurso
  - `404 Not Found` — recurso não existe (não usar para lista vazia — lista vazia retorna `200` com `[]`)
  - `409 Conflict` — conflito de estado (ex: recurso já existe)
- **DTOs sempre nas bordas:** controllers nunca expõem entidades do banco diretamente. Toda entrada e saída usa DTOs dedicados.
