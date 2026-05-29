# ClubeBeneficios.Loyalty.Worker

Worker responsavel por executar automaticamente o motor de fidelidade.

## Responsabilidade

- Processar linhas ETL elegiveis.
- Criar eventos de fidelidade.
- Recalcular saldos.
- Recalcular metricas.
- Reclassificar clientes.
- Executar com lock SQL para evitar concorrencia.

## Fora de escopo

- Ler planilhas.
- Fazer ingestao ETL.
- Atender requisicoes HTTP.
- Servir dashboards para o front.

## Execucao local

dotnet run --project .\src\ClubeBeneficios.Loyalty.Worker\ClubeBeneficios.Loyalty.Worker.csproj

## Modos

- Manual: executa uma vez e encerra.
- Watch: executa continuamente conforme PollingIntervalSeconds.

## Procedures chamadas

- dbo.usp_loyalty_process_pending_etl_rows
- dbo.usp_customer_loyalty_balance_rebuild_all
- dbo.usp_loyalty_metrics_rebuild_batch
- dbo.usp_loyalty_reclassify_batch_by_latest_metrics
- opcional: dbo.usp_customer_loyalty_expire_inactive_points
