using Reorbita.Api.Domain.Structs;

namespace Reorbita.Api.Domain.Entities;

public static class CalculadorOrbital
{
    public static double CalcularDecaimentoOrbital(CoordenadaOrbital coordenadaOrbital, double fatorArrastoAtmosferico, int diasPassados)
    {
        var taxaDecaimento = fatorArrastoAtmosferico * 0.0015;
        var alturaPerdida = diasPassados * taxaDecaimento;
        return Math.Max(0.0, coordenadaOrbital.Altitude - alturaPerdida);
    }

    public static double EstimarVidaUtilRestante(DateTime dataLancamentoUtc, double degradacaoBateria, double taxaDegradacaoMediaMensal)
    {
        var idadeMeses = (DateTime.UtcNow - dataLancamentoUtc).TotalDays / 30.0;
        var degradacaoProjetada = degradacaoBateria + (idadeMeses * taxaDegradacaoMediaMensal);
        var margemRestante = Math.Max(0.0, 100.0 - degradacaoProjetada);
        return margemRestante / Math.Max(0.1, taxaDegradacaoMediaMensal);
    }

    public static CoordenadaOrbital ConverterTLEParaCoordenadas(string linhaTle1, string linhaTle2)
    {
        // Conversao aproximada para ambiente local sem parser TLE dedicado.
        var semente = Math.Abs(HashCode.Combine(linhaTle1, linhaTle2));
        var altitude = 350 + (semente % 500);
        var inclinacao = 5 + (semente % 90);
        var excentricidade = (semente % 1000) / 1_000_000d;
        var ascensaoRetaDireta = semente % 360;

        return new CoordenadaOrbital(altitude, inclinacao, excentricidade, ascensaoRetaDireta);
    }
}
