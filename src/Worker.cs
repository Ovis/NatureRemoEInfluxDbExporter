using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NatureRemoEInfluxDbExporter.Models;
using NatureRemoEInfluxDbExporter.Options;
using ZLogger;

namespace NatureRemoEInfluxDbExporter
{
    public class Worker(
        ILogger<Worker> logger,
        IOptions<NatureRemoOption> options,
        NatureRemoEClient client,
        InfluxDbSender influxDbSender) : BackgroundService
    {
        private readonly NatureRemoOption _option = options.Value;

        private const string Measurement = "EchonetLite";

        /// <summary>
        /// 前回の電力差分計算日時
        /// </summary>
        private DateTimeOffset? LastCalculateEnergyDifference { get; set; }

        /// <summary>
        /// 前回の積算電力量生値
        /// </summary>
        private double PreviousEnergy { get; set; }


        /// <summary>
        /// ワーカー処理
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var dt = DateTimeOffset.UtcNow;

                try
                {
                    var (isSuccess, json, err) = await client.GetAppliancesAsync(stoppingToken);

                    if (!isSuccess)
                    {
                        logger.ZLogWarning(err, $"Failed to get api result.");

                        await Task.Delay(_option.Interval * 1000, stoppingToken);

                        continue;
                    }

                    var jsonDocument = JsonDocument.Parse(json);

                    var smartMeterElement = jsonDocument.RootElement[0].GetProperty("smart_meter");
                    var smartMeter = JsonSerializer.Deserialize<SmartMeterJsonResult>(smartMeterElement.GetRawText());

                    if (smartMeter is null)
                    {
                        logger.ZLogWarning($"Failed to deserialize smart meter.");
                        await Task.Delay(_option.Interval * 1000, stoppingToken);
                        continue;
                    }

                    // 値の詰込み
                    var echonetLiteValue = new EchonetLiteValue();
                    foreach (var property in smartMeter.Properties)
                    {
                        switch (property.Epc)
                        {
                            case (int)EchonetEpcEnum.Coefficient:
                                echonetLiteValue.Coefficient = int.Parse(property.Value, CultureInfo.InvariantCulture);
                                break;

                            case (int)EchonetEpcEnum.CumulativeElectricEnergyEffectiveDigits:
                                echonetLiteValue.CumulativeElectricEnergyEffectiveDigits =
                                    int.Parse(property.Value, CultureInfo.InvariantCulture);
                                break;

                            case (int)EchonetEpcEnum.NormalDirectionCumulativeElectricEnergy:
                                echonetLiteValue.NormalDirectionCumulativeElectricEnergy =
                                    double.Parse(property.Value, CultureInfo.InvariantCulture);
                                break;

                            case (int)EchonetEpcEnum.CumulativeElectricEnergyUnit:
                                echonetLiteValue.CumulativeElectricEnergyUnit = property.Value;
                                break;

                            case (int)EchonetEpcEnum.ReverseDirectionCumulativeElectricEnergy:
                                echonetLiteValue.ReverseDirectionCumulativeElectricEnergy =
                                    double.Parse(property.Value, CultureInfo.InvariantCulture);
                                break;

                            case (int)EchonetEpcEnum.MeasuredInstantaneous:
                                echonetLiteValue.MeasuredInstantaneous =
                                    double.Parse(property.Value, CultureInfo.InvariantCulture);
                                break;
                        }
                    }

                    // 生値をInfluxDBに渡す
                    {
                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.Coefficient),
                            value: echonetLiteValue.Coefficient,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.CumulativeElectricEnergyEffectiveDigits),
                            value: echonetLiteValue.CumulativeElectricEnergyEffectiveDigits,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.NormalDirectionCumulativeElectricEnergy),
                            value: echonetLiteValue.NormalDirectionCumulativeElectricEnergy,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.CumulativeElectricEnergyUnit),
                            value: echonetLiteValue.CumulativeElectricEnergyUnit,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.ReverseDirectionCumulativeElectricEnergy),
                            value: echonetLiteValue.ReverseDirectionCumulativeElectricEnergy,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: nameof(echonetLiteValue.MeasuredInstantaneous),
                            value: echonetLiteValue.MeasuredInstantaneous,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);
                    }

                    // 初回起動時用の初期化処理
                    if (LastCalculateEnergyDifference == null)
                    {
                        LastCalculateEnergyDifference = dt;
                        PreviousEnergy = echonetLiteValue.NormalDirectionCumulativeElectricEnergy;
                    }

                    // LastCalculateEnergyDifferenceが一時間前より前であれば差分を計算してInfluxDBに渡す
                    if (LastCalculateEnergyDifference.Value.AddHours(1) < dt)
                    {
                        var energyDifference = EnergyCalculator.CalculateEnergyDifference(
                            currentEnergy: echonetLiteValue.NormalDirectionCumulativeElectricEnergy,
                            previousEnergy: PreviousEnergy,
                            effectiveDigits: echonetLiteValue.CumulativeElectricEnergyEffectiveDigits,
                            coefficient: echonetLiteValue.Coefficient,
                            cumulativeElectricEnergyUnit: echonetLiteValue.CumulativeElectricEnergyUnit
                        );

                        await influxDbSender.SendTelemetryAsync(
                            measurement: Measurement,
                            field: "EnergyDifference",
                            value: energyDifference,
                            tags: [],
                            dt: dt,
                            ct: stoppingToken);

                        LastCalculateEnergyDifference = dt;
                        PreviousEnergy = echonetLiteValue.NormalDirectionCumulativeElectricEnergy;
                    }
                }
                catch (Exception ex)
                {
                    logger.ZLogError(ex, $"An error occurred while processing.");
                }

                await Task.Delay(_option.Interval * 1000, stoppingToken);
            }
        }
    }
}
