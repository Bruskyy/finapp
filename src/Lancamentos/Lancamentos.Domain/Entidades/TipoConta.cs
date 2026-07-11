namespace Lancamentos.Domain.Entidades;

/// <summary>
/// Discriminador simples em vez de herança EF (TPH/TPT): o comportamento que
/// diverge entre os tipos cabe em invariantes de factory e num método de
/// cálculo — herança de entidade adicionaria complexidade de mapeamento pra
/// ganhar um polimorfismo que nenhum consumidor precisa (ver
/// ITEM-CARTAO-CREDITO.md, decisão 1).
/// </summary>
public enum TipoConta
{
    Corrente = 1,
    Cartao = 2,
}
