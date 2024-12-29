namespace NatureRemoEInfluxDbExporter.Models
{
    public class EchonetLiteValue
    {
        /// <summary>
        /// 正方向積算電力量 0xE0(224)
        /// </summary>
        public double NormalDirectionCumulativeElectricEnergy { get; set; }

        /// <summary>
        /// 逆方向積算電力量 0xE3(227)
        /// </summary>
        public double ReverseDirectionCumulativeElectricEnergy { get; set; }

        /// <summary>
        /// 係数 0xD3(211)
        /// </summary>
        public int Coefficient { get; set; }

        /// <summary>
        /// 積算電力量有効桁数 0xD7(215)
        /// </summary>
        public int CumulativeElectricEnergyEffectiveDigits { get; set; }

        /// <summary>
        /// 積算電力量単位 0xE1(225)
        /// </summary>
        public string CumulativeElectricEnergyUnit { get; set; } = string.Empty;

        /// <summary>
        /// 瞬時電力計測値 0xE7(231)
        /// </summary>
        public double MeasuredInstantaneous { get; set; }


    }
}
