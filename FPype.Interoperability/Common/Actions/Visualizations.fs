namespace FPype.Interoperability.Common.Actions


module Visualizations =

    open System.Text.Json
    open FsToolbox.Core
    open System.Text.Json.Serialization
    open FPype.Actions

    type ValueRangeItem =
        { [<JsonPropertyName("isUnits")>]
          IsUnits: bool
          [<JsonPropertyName("value")>]
          Value: double }

        member this.WriteToJsonProperty(name, writer) =
            Json.writePropertyObject
                (fun w ->
                    match this.IsUnits with
                    | true ->
                        w.WriteString("type", "unitSize")
                        w.WriteNumber("size", this.Value)
                    | false ->
                        w.WriteString("type", "specific")
                        w.WriteNumber("value", this.Value))
                name
                writer

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

        interface IPipelineAction with

            [<JsonPropertyName "actionType">]
            member this.ActionType = nameof this

            member this.GetActionName() =
                Visualizations.``generate-time-series-chart-collection``.name

            member this.ToSerializedActionParameters() = failwith "todo"

    and TimeSeriesChartGeneratorSettings =
        { [<JsonPropertyName "timestampValueIndex">]
          TimestampValueIndex: int
          [<JsonPropertyName "timestampFormat">]
          TimestampFormat: string
          [<JsonPropertyName "minimumValue">]
          MinimumValue: ValueRangeItem
          [<JsonPropertyName "maximumValue">]
          MaximumValue: ValueRangeItem

        (*
    [JsonPropertyName("chartSettings")] public TimeSeriesChartSettings ChartSettings { get; set; } = new();

    [JsonPropertyName("seriesSettings")]
    public IEnumerable<TimeSeriesSeriesSettings> SeriesSettings { get; set; } = new List<TimeSeriesSeriesSettings>();
        *)

        }

        member this.WriteToJsonProperty(name, writer) =
            Json.writePropertyObject
                (fun w ->
                    w.WriteNumber("timestampValueIndex", this.TimestampValueIndex)
                    w.WriteString("timestampFormat", this.TimestampFormat)
                    this.MinimumValue.WriteToJsonProperty("minimumValue", w)
                    this.MaximumValue.WriteToJsonProperty("maximumValue", w)
                    
                    )
                name
                writer

    and TimeSeriesChartSettings = {
        [<JsonPropertyName "height">]
        Height: double option
        [<JsonPropertyName "width">]    
        Width: double option
        [<JsonPropertyName "bottomOffset">]    
        BottomOffset: double option
        []
        
        (*
        [JsonPropertyName("height")] public double? Height { get; set; }

    [JsonPropertyName("width")] public double? Width { get; set; }

    [JsonPropertyName("bottomOffset")] public double? BottomOffset { get; set; }

    [JsonPropertyName("topOffset")] public double? TopOffset { get; set; }

    [JsonPropertyName("leftOffset")] public double? LeftOffset { get; set; }

    [JsonPropertyName("rightOffset")] public double? RightOffset { get; set; }

    [JsonPropertyName("legendPosition")] public LegendPosition LegendPosition { get; set; } = LegendPosition.None;

    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;

    [JsonPropertyName("xLabel")] public string XLabel { get; set; } = string.Empty;

    [JsonPropertyName("yLabel")] public string YLabel { get; set; } = string.Empty;

    [JsonPropertyName("yMajorMarkers")] public IEnumerable<double> YMajorMarkers { get; set; } = new List<double>();

    [JsonPropertyName("yMinorMarkers")] public IEnumerable<double> YMinorMarkers { get; set; } = new List<double>();
        *)
    }
    
        member this.WriteToJsonProperty(name, writer) =
            ()

