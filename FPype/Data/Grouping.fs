namespace FPype.Data

module Grouping =

    open System

    type DateGroups =
        { FieldName: string
          Label: string
          Groups: DateGroup list }

    and DateGroup =
        { StartDate: DateTime
          EndDate: DateTime
          Label: string
          StartInclusive: bool
          EndInclusive: bool }

        static member GenerateWeekGroups(fromDt: DateTime, weeks) =
            let startDt =
                fromDt.AddDays((int fromDt.DayOfWeek - 1) * -1 |> float)

            [ 0..weeks ]
            |> List.fold
                (fun (acc, lastDt) i ->
                    acc
                    @ [ { StartDate = lastDt
                          EndDate = lastDt.AddDays(6.)
                          Label = $"Week {i}"
                          StartInclusive = true
                          EndInclusive = true } ],
                    lastDt.AddDays(7.))
                ([], startDt)
            |> fun (acc, _) -> acc

        static member GenerateMonthGroups(fromDt: DateTime, months) =
            let startDt =
                DateTime(fromDt.Year, fromDt.Month, 1)

            [ 0..months ]
            |> List.fold
                (fun (acc, lastDt) i ->
                    acc
                    @ [ { StartDate = lastDt
                          EndDate = lastDt.AddMonths 1
                          Label = lastDt.ToString("MMM yy") // $"{lastDt:MMM-yy}"
                          StartInclusive = true
                          EndInclusive = false } ],
                    lastDt.AddMonths 1)
                ([], startDt)
            |> fun (acc, _) -> acc

    type CategoryGroups =
        { FieldName: string
          Groups: CategoryGroup list }

    and CategoryGroup = { Name: string }

