using System.Threading.Channels;
using CashFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace CashFlow.Infrastructure.Messaging;

/// <summary>
/// Fila produtor/consumidor em memória, baseada em <see cref="Channel{T}"/>.
///
/// Racional de projeto (documentado aqui e referenciado no README):
/// registrar um lançamento (o caminho de escrita do qual o negócio depende
/// a cada venda) nunca pode ficar mais lento ou ser bloqueado pelo subsistema
/// de consolidação diária. Usamos um canal limitado (bounded channel) com
/// <see cref="BoundedChannelFullMode.Wait"/> combinado a <c>TryWrite</c>
/// (nunca <c>WriteAsync</c>) para obter:
///   - Produtores não bloqueantes: <see cref="TryEnqueue"/> nunca aguarda e
///     nunca lança exceção; retorna <c>false</c> em vez de bloquear quando a
///     fila está saturada, então um consumidor lento/degradado nunca deixa o
///     RegisterLaunchCommandHandler mais lento.
///     (Atenção: <see cref="BoundedChannelFullMode.DropWrite"/> pareceria a
///     opção óbvia pelo nome, mas com esse modo o próprio .NET documenta que
///     <c>TryWrite</c> sempre retorna <c>true</c> - ele descarta o item mais
///     antigo da fila, nunca a escrita atual. Isso quebraria silenciosamente
///     o contrato "retorna false quando saturado" que o resto do sistema
///     depende para logar e para os testes de unidade validarem.)
///   - Uma válvula natural de contrapressão: no pico documentado de 50
///     req/s, com até 5% de perda aceitável, uma capacidade limitada
///     descarta intencionalmente o excesso em vez de crescer a memória sem
///     limite durante uma explosão de tráfego.
///   - Um único consumidor dedicado em background (ver ConsolidationWorker)
///     aplica as políticas de retry e circuit breaker sem nunca tocar no
///     caminho da requisição HTTP.
///
/// Este é, explicitamente, um mecanismo em memória e de processo único. Está
/// documentado no README como o ponto de extensão para substituir por um
/// broker real (RabbitMQ/Azure Service Bus/SQS) caso a aplicação precise
/// escalar horizontalmente ou precise de entrega durável entre reinícios.
/// </summary>
public sealed class InMemoryConsolidationQueue : IConsolidationQueue
{
    // Capacidade dimensionada bem acima do pico documentado (50 req/s) para
    // que apenas uma sobrecarga genuinamente sustentada descarte sinais -
    // cada sinal descartado apenas atrasa a visibilidade de um saldo, nunca
    // perde um lançamento.
    private const int Capacity = 200;

    private readonly Channel<DateOnly> _channel;
    private readonly ILogger<InMemoryConsolidationQueue> _logger;

    public InMemoryConsolidationQueue(ILogger<InMemoryConsolidationQueue> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<DateOnly>(new BoundedChannelOptions(Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });
    }

    public bool TryEnqueue(DateOnly date)
    {
        var accepted = _channel.Writer.TryWrite(date);

        if (!accepted)
        {
            _logger.LogWarning("Fila de consolidação em capacidade máxima ({Capacity}); sinal descartado para {Date}.", Capacity, date);
        }

        return accepted;
    }

    public IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
