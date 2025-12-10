using System.ComponentModel;

namespace Domain.Enums;

public enum RiskCategory
{
    [Description("Trades com valor menor que 1.000.000")]
    LOWRISK,

    [Description("Trades com valor maior ou igual a 1.000.000 e cliente do setor Público")]
    MEDIUMRISK,

    [Description("Trades com valor maior ou igual a 1.000.000 e cliente do setor Privado")]
    HIGHRISK
}
