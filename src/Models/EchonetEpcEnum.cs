namespace NatureRemoEInfluxDbExporter.Models
{
    public enum EchonetEpcEnum
    {
        /// <summary>
        /// 係数
        /// </summary>
        Coefficient = 211,

        /// <summary>
        /// 積算電力量有効桁数
        /// </summary>
        CumulativeElectricEnergyEffectiveDigits = 215,

        /// <summary>
        /// 正方向積算電力量
        /// </summary>
        NormalDirectionCumulativeElectricEnergy = 224,

        /// <summary>
        /// 積算電力量単位
        /// </summary>
        CumulativeElectricEnergyUnit = 225,

        /// <summary>
        /// 逆方向積算電力量
        /// </summary>
        ReverseDirectionCumulativeElectricEnergy = 227,

        /// <summary>
        /// 瞬時電力計測値
        /// </summary>
        MeasuredInstantaneous = 231
    }
}
