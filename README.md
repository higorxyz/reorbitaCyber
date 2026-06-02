<h1><img src="assets/brand/reorbita-logo.png" alt="Logo REORBITA" width="56" align="absmiddle" /> REORBITA - Cybersecurity (Global Solution 2026/1)</h1>

Documento único de Cybersecurity da REORBITA, consolidando:
- o que foi implementado na API em C#,
- as respostas diretas às questões de segurança da entrega,
- e a rastreabilidade entre riscos, controles e resiliência operacional.

## 1. Contexto e objetivo
Este entregável cobre a camada de segurança da API REORBITA, voltada a monitoramento preditivo de satélites e coordenação de intervenções de frota orbital.

Objetivo principal: garantir operação resiliente, confiável e aderente a boas práticas de segurança da informação para o ecossistema proposto.

### 1.1 Ideia do REORBITA, problema e aplicação da solução

Ideia da iniciativa:
- Construir um ecossistema de manutenção orbital que combine telemetria, análise preditiva e execução de intervenções técnicas para prolongar a vida útil de satélites.

Problema enfrentado:
- Satélites de alto valor tendem a perder desempenho por falhas pontuais, degradação de bateria, desgaste de subsistemas e queda de eficiência operacional.
- Sem monitoramento e resposta antecipada, o risco de indisponibilidade aumenta, elevando perdas econômicas e contribuindo para descarte prematuro de ativos em órbita.

Como a solução se aplica:
- A API REORBITA recebe e valida telemetria em tempo real, identifica padrões de risco e gera alertas acionáveis.
- Para cenários críticos, a plataforma suporta orquestração de comandos de intervenção da frota orbital com controles de autorização e trilha de auditoria.
- A camada de cybersecurity documentada aqui garante que esse fluxo operacional ocorra com proteção de dados, controle de acesso, integridade de informações e capacidade de resposta a incidentes.

Este documento foi estruturado para responder diretamente aos requisitos de segurança da entrega:
- Análise de Riscos e Ameaças (Threat Modeling)
- Arquitetura de Segurança (Controles)
- Governança e Compliance
- Plano de Resiliência e Continuidade

## 2. Integrantes
| Nome | RM |
|---|---|
| Bento Rangel | RM559124 |
| Eric Yuji | RM554869 |
| Higor Batista | RM558907 |
| Kaue Pires | RM554403 |
| Ricardo Di Tilia | RM555155 |

## 3. Mapa de aderência dos requisitos de segurança
| Requisito de segurança | Onde está respondido neste documento |
|---|---|
| 1.1 Identificação de Ativos | Seção 4.1 |
| 1.2 Modelo de Ameaças (mínimo 3 vetores) | Seção 4.1 |
| 2.1 Controles de Acesso | Seção 4.2 |
| 2.2 Proteção de Dados (trânsito e repouso) | Seção 4.2 |
| 2.3 Segurança da Infraestrutura | Seção 4.2 |
| 3.1 Alinhamento Normativo (ISO 27001) | Seção 4.3 |
| 3.2 Privacidade (LGPD/princípios similares) | Seção 4.3 |
| 4.1 Plano de Resposta a Incidentes | Seção 4.4 |

## 4. Respostas objetivas dos requisitos de segurança

### 4.1 Análise de Riscos e Ameaças (Threat Modeling)

#### 1.1 Identificação de Ativos
Questão orientadora: Quais são os dados críticos e infraestruturas manipulados?
Resposta:

Ativos de dados:
| Ativo | Onde aparece na implementação | Criticidade |
|---|---|---|
| Telemetria em tempo real | TelemetriaController, LeituraTelemetria, ServicoMonitoramento | Alta |
| Histórico de telemetria | Satelite.Telemetria, ServicoMonitoramento.Historico | Alta |
| Previsões de falha | MotorPreditivoReorbita, PrevisaoFalha | Alta |
| Dados de operadoras | Claims JWT + validações por operadora nos serviços | Alta |
| Ordens de serviço da frota | OrdemServico, ServicoFrota, FrotaController | Alta |
| Credenciais e tokens | ServicoAutenticacao, IHashCredencial, JWT, TokenRevogacaoStore | Crítica |
| Logs de auditoria | ILogger + AddJsonConsole | Alta |

Ativos de infraestrutura:
| Ativo | Onde aparece na implementação | Criticidade |
|---|---|---|
| API REST | Controllers, Program, middleware global | Crítica |
| Persistência local | RepositorioSateliteArquivo, RepositorioFrotaArquivo | Alta |
| Canal de ingestão de telemetria | TelemetriaController + Rate Limiter | Alta |
| Canal de comando da frota | FrotaController + ValidacaoMtlsCanalRobosMiddleware | Crítica |
| Transporte HTTPS/TLS | Kestrel com TLS 1.3 + UseHttpsRedirection | Alta |

#### 1.2 Modelo de Ameaças
Questão orientadora: Quais vetores de ataque plausíveis para a solução (mínimo 3)?
Resposta:
| Vetor | Categoria STRIDE | Risco principal | Mitigação implementada |
|---|---|---|---|
| Manipulação de telemetria | Tampering | Decisão operacional incorreta | Validação de faixa física + tratamento de TelemetriaInvalidaException |
| Interceptação/alteração de comando | Information Disclosure/Tampering | Comando malicioso de intervenção | HTTPS/TLS + mTLS configurável + RBAC + policy FrotaComando |
| DDoS na ingestão | Denial of Service | Indisponibilidade da API | AddRateLimiter + EnableRateLimiting no endpoint de telemetria |
| Escalada de privilégio | Elevation of Privilege | Acesso indevido a comandos críticos | JWT + roles + MFA para ReorbitaAdmin em comandos de frota |
| Corrupção da persistência | Tampering | Estado adulterado de satélites/robôs | SHA-256 de integridade + exceção de integridade + logs críticos |

### 4.2 Arquitetura de Segurança (Controles)

#### 2.1 Controles de Acesso
Questão orientadora: Como garantir que apenas usuários/sistemas autorizados acessem os dados?
Resposta:
- Autenticação JWT com validação de emissor, audiência, assinatura e expiração.
- RBAC por papel (OperadoraLeitura, OperadoraEscrita, OperadoraAdmin, ReorbitaAdmin, RoboSistema).
- Privilégio mínimo por operadora (escopo por claim de operadora).
- Policy FrotaComando: OperadoraAdmin pode executar comando de intervenção.
- Policy FrotaComando: ReorbitaAdmin só executa com claim mfa=true.
- Policy AdminComMfa aplicada no endpoint administrativo de revogação.
- Revogação operacional de token com verificação de jti.
- Endpoint: POST /api/auth/revogar-atual.
- Endpoint: POST /api/auth/revogar.
- Checagem no pipeline JWT para bloquear token revogado.

#### 2.2 Proteção de Dados
Questão orientadora: Como os dados são protegidos em trânsito e em repouso?
Resposta:
- Em trânsito: TLS 1.3 no Kestrel + redirecionamento HTTPS.
- Canal sensível da frota: mTLS (habilitação por configuração) para /api/frota/intervencao.
- Em repouso: criptografia AES para payload de persistência.
- Integridade: hash SHA-256 validado em leitura/escrita de arquivos.
- Credenciais: bcrypt via IHashCredencial/BcryptHashCredencial.
- Entrada: validação server-side de telemetria em faixa física esperada.

#### 2.3 Segurança da Infraestrutura
Questão orientadora: Quais tecnologias asseguram que o sistema seja íntegro?
Resposta:
- Arquitetura de autenticação/autorização centralizada no Program.
- Middleware global para tratamento de exceções.
- Logs estruturados em JSON para observabilidade e trilha forense.
- Eventos críticos de segurança via LogCritical.
- Rate limiting para proteção de disponibilidade em ingestão.
- Camada de perímetro prevista no plano (firewall/reverse proxy) para mitigação de abuso de tráfego.

### 4.3 Governança e Compliance

#### 3.1 Alinhamento Normativo (ISO 27001)
Questão orientadora: Como o projeto trata Gestão de Riscos e Segurança da Informação?
Resposta:
Mapeamento por princípio CIA:
- Confidencialidade: JWT, RBAC, isolamento por operadora, endpoint administrativo com MFA.
- Integridade: SHA-256 de persistência, controles de exceção de integridade, validações server-side.
- Disponibilidade: rate limiting, tratamento global de exceções e degradação controlada por código HTTP.

Referência de controles do Anexo A aplicada na solução:
- A.9 Controle de Acesso: autenticação JWT, RBAC por papéis, policies de autorização e escopo por operadora.
- A.10 Criptografia: TLS 1.3 em trânsito, AES em repouso, bcrypt para segredo de acesso e SHA-256 para integridade.
- A.12 Segurança Operacional: logs estruturados, middleware global de exceções e rate limiting de ingestão.
- A.16 Gestão de Incidentes: fluxo definido de detecção, contenção, erradicação e recuperação (seção 4.4).

Mapeamento PDCA:
- Plan: modelagem de ameaças e definição de controles.
- Do: implementação em Program, middleware, serviços e repositórios.
- Check: build, logs estruturados e monitoramento de eventos críticos.
- Act: plano de resposta a incidentes e backlog de evolução.

#### 3.2 Privacidade (LGPD/princípios similares)
Questão orientadora: Como a solução protege dados de localização/comportamentais?
Resposta:
- Dados de localização operacional tratados: coordenadas orbitais, histórico de telemetria e trilhas de comando por satélite/operadora.
- Dados comportamentais operacionais tratados: padrões de anomalia, tendências de falha e histórico de intervenções.
- Finalidade e necessidade: coleta mínima para monitoramento e manutenção orbital.
- Minimização de exposição: filtros por operadora nos endpoints de consulta/ação.
- Transparência: respostas padronizadas de erro e trilhas de log para auditoria.
- Segurança: JWT, RBAC, MFA em contexto administrativo, AES, SHA-256, bcrypt e mTLS configurável.
- Responsabilização: trilhas de eventos técnicos e de segurança com logger estruturado.

### 4.4 Plano de Resiliência e Continuidade

#### 4.1 Plano de Resposta a Incidentes
Questão orientadora: O que acontece se o sistema sofrer invasão?
Resposta (fluxo operacional):

Detecção:
- monitoramento de logs JSON,
- foco em eventos críticos,
- correlação entre falhas de autorização, integridade e disponibilidade.

Contenção (0 a 15 min):
- revogar tokens comprometidos,
- bloquear origem suspeita no perímetro,
- suspender comando de frota,
- restringir operadora impactada ao modo de menor privilégio.

Contenção (15 a 60 min):
- isolar componente afetado,
- preservar evidência (snapshot e hashes),
- comunicar stakeholders impactados.

Erradicação:
- análise forense,
- correção da vulnerabilidade,
- rotação de segredos,
- revalidação do ambiente.

Recuperação:
- retorno gradual (leitura -> telemetria -> comandos),
- reautenticação controlada,
- atualização de modelos de ameaça e runbooks.

## 5. Matriz de rastreabilidade (risco x controle x implementação)
| Risco | Controle | Implementação na API |
|---|---|---|
| DDoS em telemetria | Rate limiting | AddRateLimiter + EnableRateLimiting |
| Comando não autorizado | RBAC + policy + MFA condicional | FrotaComando + AdminComMfa |
| Token comprometido | Revogação por jti | TokenRevogacaoStore + endpoints de revogação + checagem JWT |
| Corrupção de persistência | Integridade criptográfica | IntegridadeArquivoHelper + exceção de integridade |
| Exposição de dados em disco | Criptografia em repouso | CriptografiaArquivoHelper (AES) |
| Falta de trilha forense | Logging estruturado | AddJsonConsole + ILogger + LogCritical |

## 6. Pontos de evolução (maturidade)
- Persistir revogação de token em armazenamento durável para cenários multi-instância.
- Instrumentar SIEM/alertas automáticos com limiares por evento crítico.
- Formalizar exercícios periódicos de mesa (tabletop) para incidente cibernético.

## 7. Conclusão
O documento reúne a visão de risco, os controles aplicados e o plano de continuidade em um único artefato técnico e auditável.
