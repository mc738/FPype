namespace FPype.Interoperability.Common.Actions

open System.Text.Json.Serialization

module Visualizations =

    type ValueRangeItem =
        { [<JsonPropertyName("isUnits")>]
          IsUnits: bool
          [<JsonPropertyName("value")>]
          Value: double }

    type GenerateTimeSeriesChartCollectionAction =
        { [<JsonPropertyName "resultBucket">]
          ResultBucket: string
          [<JsonPropertyName "fileNameFormat">]
          FileNameFormat: string
          [<JsonPropertyName "categoriesQuery">]
          CategoriesQuery: ActionQueryVersion
          [<JsonPropertyName "categoriesTable">]
          CategoriesTable: ActionTableVersion
          [<JsonPropertyName "categoryIndex">]
          CategoryIndex: int
          [<JsonPropertyName "timeSeriesQuery">]
          TimeSeriesQuery: ActionQueryVersion
          [<JsonPropertyName "timeSeriesTable">]
          TimeSeriesTable: ActionTableVersion

        (*
        
    [JsonPropertyName("resultBucket")] public string ResultBucket { get; set; } = string.Empty;

    [JsonPropertyName("fileNameFormat")] public string FileNameFormat { get; set; } = string.Empty;

    [JsonPropertyName("categoriesQuery")] public ActionQueryVersion CategoriesQuery { get; set; } = new();

    [JsonPropertyName("categoriesTable")] public ActionTableVersion CategoriesTable { get; set; } = new();

    [JsonPropertyName("categoryIndex")] public int CategoryIndex { get; set; }

    [JsonPropertyName("timeSeriesQuery")] public ActionQueryVersion TimeSeriesQuery { get; set; } = new();

    [JsonPropertyName("timeSeriesTable")] public ActionTableVersion TimeSeriesTable { get; set; } = new();

    [JsonPropertyName("settings")] public TimeSeriesChartGeneratorSettings Settings { get; set; } = new();
    *)
        }

    and TimeSeriesChartGeneratorSettings =
        { [<JsonPropertyName "timestampValueIndex">]
          TimestampValueIndex: int
          [<JsonPropertyName "timestampFormat">]
          TimestampFormat: string

        (*
            [JsonPropertyName("timestampValueIndex")]
    public int TimestampValueIndex { get; set; }

    [JsonPropertyName("timestampFormat")] public string TimestampFormat { get; set; } = string.Empty;

    [JsonPropertyName("minimumValue")] public ValueRangeItem MinimumValue { get; set; } = new();

    [JsonPropertyName("maximumValue")] public ValueRangeItem MaximumValue { get; set; } = new();

    [JsonPropertyName("chartSettings")] public TimeSeriesChartSettings ChartSettings { get; set; } = new();

    [JsonPropertyName("seriesSettings")]
    public IEnumerable<TimeSeriesSeriesSettings> SeriesSettings { get; set; } = new List<TimeSeriesSeriesSettings>();
        *)

        }

    ()
