# stock-quote-alert
Alerta para compra e venda de ações
- Programa para enviar email para avisar quando o preço de uma ação está acima do preço de venda informado ou abaixo do preço de compra informado.

### TODO

- [X] Adicionar dispose do SmtpClient e MailMessages
- [X] Criar função para validar os parâmetros de entrada.
- [X] Criar função destinada a mandar os emails.
- [X] Criar StockConverter que herda de JsonConverter, responsável pela conversão do JSON da ação
- [X] Trocar backoff de email para delays com cancellation token
- [X] Adicionar paralelismo no envio de email.
- [X] Adicionar CancellationToken para encerrar programa
- [X] Adicionar projeto de testes unitários
- [ ] Adicionar dicionário de exceções
- [ ] Listar ações que a API atende no começo da execução
- [ ] Validar código da ação em um dicionário de ações disponíveis
- [ ] Montar um HTML e compra e um de venda pra ser enviado no email
- [ ] Mostrar intervalo de espera durante a execução
- [ ] Mudar API consultada para API da B3
