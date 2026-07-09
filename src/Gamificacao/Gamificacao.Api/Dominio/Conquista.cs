namespace Gamificacao.Api.Dominio;

/// <summary>
/// Catálogo de conquistas (BACKLOG-PRODUTO.md, Onda 1, item 5) — dado de
/// referência fixo, seedado via migration (mesmo padrão das Categorias em
/// Lancamentos.Api). Não confundir com <see cref="UsuarioConquista"/>, que é
/// o registro de desbloqueio por usuário.
/// </summary>
public class Conquista
{
    public Guid Id { get; private set; }

    /// <summary>Chave de negócio estável (usada em código, ex: ConquistaCodigos.PrimeiroSalario) - independente do Guid gerado no seed.</summary>
    public string Codigo { get; private set; }

    public string Nome { get; private set; }
    public string Descricao { get; private set; }

    /// <summary>Nome de ícone do Ionicons - o backend decide o ícone, o cliente só exibe.</summary>
    public string Icone { get; private set; }

    private Conquista() { Codigo = Nome = Descricao = Icone = null!; }

    public Conquista(Guid id, string codigo, string nome, string descricao, string icone)
    {
        Id = id;
        Codigo = codigo;
        Nome = nome;
        Descricao = descricao;
        Icone = icone;
    }
}
