# ⚔️ IARunage: Manopla de Prodígios

Um sistema avançado de simulação de combate e Inteligência Artificial baseado em **Aprendizado por Reforço (Linear Q-Learning)**, construído para dar vida a confrontos dinâmicos no universo de **Runage**. 

Neste projeto, tanto o jogador quanto os inimigos (NPCs) são agentes de IA que aprendem a lutar, sobreviver e reagir a diferentes cenários de combate em turnos através de milhares de simulações. Desenvolvido inteiramente em **C# (.NET 10)**, o sistema não utiliza bibliotecas externas de Machine Learning, implementando a matemática do algoritmo Q-Learning de forma orgânica e controlada.

---

## 🧠 Características Principais

* **Q-Learning do Zero:** Algoritmo de aprendizado por reforço implementado na classe `QAgent`, com controle dinâmico de taxa de exploração, desconto e temperatura.
* **Múltiplos Módulos de Execução:** Ambientes separados para testes locais, treinamento massivo (geração de pesos) e aplicação prática do modelo.
* **Sistema de Viés Moral (Aura):** O Player IA se adapta com base em um valor de Viés (0.0 a 1.0), onde `0.0` gera um comportamento pacífico/defensivo e `1.0` gera um comportamento implacável e caótico.
* **Mente Colmeia (Hivemind) Inimiga:** Os mobs são gerados com arquétipos comportamentais próprios (Oportunista, Vingativo, Reativo) e treinam matrizes neurais específicas para neutralizar o viés do jogador.
* **Combate Tático:** Gerenciamento de distância, cooldown de habilidades (Sobrecarga, Runa de Fogo), mecânicas de *Parry* e punições rigorosas para ataques injustificados ou covardia.

---

## ⚙️ Módulos do Sistema

O fluxo principal (`Program.cs`) divide a aplicação em quatro pilares:

### 1. 🧪 Arena de Testes (Laboratório)
Ambiente de sandbox focado em balanceamento de Game Design. Permite testar combates isolados (ex: *Player vs Lobo Faminto*) sem salvar os pesos no disco, exibindo estatísticas de mortalidade e moralidade ao final.

### 2. 🌋 Forja Massiva (Treinamento Real)
O motor de aprendizado do jogo. Executa duas fases principais:
* **Cérebro Global:** O Player IA passa por 6.000 episódios contra uma mistura de todos os mobs para aprender o básico da sobrevivência (andar, bater, curar).
* **Especialização:** O Player treinado enfrenta Mobs recém-nascidos. Ambos evoluem simultaneamente, forjando "Cérebros" (`.json`) altamente especializados salvos no diretório `Data/`.

### 3. 🛡️ Manopla de Prodígios (Modo Sobrevivência)
O desafio final. O Player IA (carregado com os pesos da Forja) tenta sobreviver a uma masmorra procedural de 100 andares, lidando com o aumento de *status* dos inimigos, fadiga e chefes a cada 10 andares. Apenas a IA mais bem treinada consegue alcançar o topo.

### 4. 🌍 Varredura Global
Um executor rápido que coloca o Player para enfrentar todos os mobs do jogo em sequência, gerando um relatório amplo da eficiência do balanceamento atual.

---

## 👾 Bestiário e Arquétipos

O ecossistema é populado por diversas criaturas e personagens, cada um com status, recompensas e multiplicadores de combate geridos pelo `PerfilMob`:

* **Aldeão Assustado** *(Humano Comum)*
* **Cervo de Prodígios** *(Animal Indefeso)*
* **Lobo Faminto** *(Animal Agressivo)*
* **Urso Territorial** *(Animal Reativo)*
* **Bandido Oportunista** *(Oportunista - ataca e recua de forma imprevisível)*
* **Guarda Veterano** *(Humano++)*
* **Cultista Frenético** *(Inimigo)*
* **Espírito Vingativo** *(Defensor Vingativo)*

---

## 📁 Estrutura do Projeto

```text
IARunage/
├── Data/                   # Armazena os pesos (Matrizes Q-Table em .json)
│   ├── NPC/                # Matrizes neurais da Mente Colmeia de cada inimigo
│   └── Player/             # Matrizes de experiência do Player separadas por Viés
├── Engines/                # Motores de execução (Arena, Forja, Sobrevivência)
├── Models/                 # Entidades base (QAgent, CombatEnvironment, Arquetipo)
├── Utils/                  # Manipuladores de arquivos e serialização JSON
├── Program.cs              # Hub Central / Ponto de Entrada
└── LinearQLearning.csproj  # Configuração do projeto (.NET 10)

## 🚀 Como Executar

### Pré-requisitos
* SDK do [.NET 10](https://dotnet.microsoft.com/) instalado na máquina.

### Passos

1. Clone o repositório:
   ```bash
   git clone [https://github.com/lucke0302/iarunage.git](https://github.com/lucke0302/iarunage.git)
   cd iarunage
   ```

2. Restaure as dependências e compile:
   ```bash
   dotnet build
   ```

3. Execute a aplicação:
   ```bash
   dotnet run
   ```

**Recomendação de Fluxo:** Se for a primeira vez rodando, inicie pelo módulo **[2] Forja Massiva** para que os arquivos `.json` sejam gerados na pasta `Data/`. Somente depois disso o módulo **[3] Manopla de Prodígios** terá os cérebros necessários para tentar sobreviver.

---

## 🛠️ Tecnologias Utilizadas
* **Linguagem:** C# 14
* **Framework:** .NET 10.0
* **Serialização:** `System.Text.Json` para exportação de Q-Tables em formato matricial.
* **Paradigma:** Orientação a Objetos e Machine Learning Baseado em Recompensa.

---
*O destino do agente depende apenas de sua capacidade de adaptação aos perigos de Runage.*