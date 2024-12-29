using System.Globalization;

namespace NatureRemoEInfluxDbExporter
{
    public static class EnergyCalculator
    {
        /// <summary>
        /// Nature RemoのAPIから取得したデータを基に積算電力量の差分を計算
        /// </summary>
        /// <param name="currentEnergy">現在の積算電力量（0xE0）</param>
        /// <param name="previousEnergy">1時間前の積算電力量（0xE0）</param>
        /// <param name="effectiveDigits">有効桁数（0xD7 215）</param>
        /// <param name="coefficient">係数（0xD3 211)</param>
        /// <param name="cumulativeElectricEnergyUnit">積算電力量単位（0xE1、kWh単位の場合は1.0）</param>
        /// <returns>消費電力量 (kWh)</returns>
        public static double CalculateEnergyDifference(
            double currentEnergy,
            double previousEnergy,
            int effectiveDigits,
            int coefficient,
            string cumulativeElectricEnergyUnit
        )
        {
            // JSON文字列の積算電力量単位をバイト値に変換
            if (!byte.TryParse(cumulativeElectricEnergyUnit, NumberStyles.HexNumber, null, out var energyUnitByte))
            {
                throw new ArgumentException($"不正な積算電力量単位文字列: {cumulativeElectricEnergyUnit}");
            }

            // カウンタの最大値を計算（10^有効桁数 - 1）
            var maxCounterValue = Math.Pow(10, effectiveDigits) - 1;

            // 差分を計算し、オーバーフローを補正
            var difference = currentEnergy >= previousEnergy
                ? currentEnergy - previousEnergy
                : currentEnergy - previousEnergy + (maxCounterValue + 1);

            // 積算電力量単位を計算
            var energyUnit = ConvertCumulativeElectricEnergyUnit(energyUnitByte);

            // 差分に係数と単位を適用
            return difference * energyUnit * coefficient;
        }


        /// <summary>
        /// 積算電力量単位バイト値（0xD3）を実際の積算電力量単位に変換
        /// </summary>
        /// <param name="coefficientByte">積算電力量単位バイト値</param>
        /// <returns>積算電力量単位 (kWh単位)</returns>
        private static double ConvertCumulativeElectricEnergyUnit(byte coefficientByte)
        {
            return coefficientByte switch
            {
                0x00 => 1.0,        // 1kWh
                0x01 => 0.1,        // 0.1kWh
                0x02 => 0.01,       // 0.01kWh
                0x03 => 0.001,      // 0.001kWh
                0x04 => 0.0001,     // 0.0001kWh
                0x0A => 10.0,       // 10kWh
                0x0B => 100.0,      // 100kWh
                0x0C => 1000.0,     // 1000kWh
                0x0D => 10000.0,    // 10000kWh
                _ => throw new ArgumentException($"不正な積算電力量単位バイト値: {coefficientByte:X2}")
            };
        }
    }
}
