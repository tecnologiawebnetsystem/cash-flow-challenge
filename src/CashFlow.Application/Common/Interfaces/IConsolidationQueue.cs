namespace CashFlow.Application.Common.Interfaces;

/// <summary>
/// Abstração produtor/consumidor que desacopla o registro de lançamentos da
/// consolidação do saldo diário. Este é o mecanismo central por trás do
/// requisito não funcional de resiliência: registrar um lançamento nunca
/// espera por - nem falha por causa de - o subsistema de consolidação.
///
/// A fila é best-effort e limitada: sob carga de pico extrema ela pode
/// descartar um sinal de "por favor consolide esta data" (ver o retorno de
/// <see cref="TryEnqueue"/>), mas isso nunca perde dados financeiros, pois
/// os lançamentos são sempre persistidos de forma síncrona independentemente
/// do resultado da fila, e a própria consolidação é idempotente e
/// periodicamente reconciliada.
/// </summary>
public interface IConsolidationQueue
{
    /// <summary>
    /// Tenta agendar uma consolidação para <paramref name="date"/>.
    /// Retorna <c>false</c> quando a fila está cheia e a requisição foi
    /// descartada em vez de bloquear quem chamou.
    /// </summary>
    bool TryEnqueue(DateOnly date);

    IAsyncEnumerable<DateOnly> ReadAllAsync(CancellationToken cancellationToken);
}
