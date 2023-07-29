module FPype.Data.Cleansing

open FPype.Data.Cleansing

module Dsl =

    let validate (step: ValidationStep) = CleansingStep.Validate step

    let validation (steps: ValidationStep list) = steps |> List.map validate

    let transform (step: TransformationStep) = CleansingStep.Transform step

    let transformation (steps: TransformationStep list) = steps |> List.map transform

    let containsCharacters (chars: char list) =
        ValidationStep.ContainsCharacters chars |> validate

    let doesNotContainCharacters (chars: char list) =
        ValidationStep.ContainsCharacters chars |> ValidationStep.Not |> validate

    let contains (str: string) = ValidationStep.Contains str |> validate

    let doesNotContain (str: string) =
        ValidationStep.Contains str |> ValidationStep.Not |> validate

    let containsLetters = ValidationStep.ContainsLetters |> validate

    let doesNotContainLetters =
        ValidationStep.ContainsLetters |> ValidationStep.Not |> validate

    let containsNumbers = ValidationStep.ContainsNumbers |> validate

    let doesNotContainNumbers =
        ValidationStep.ContainsNumbers |> ValidationStep.Not |> validate

    let containsPunctuation = ValidationStep.ContainsPunctuation |> validate

    let doesNotContainPunctuation =
        ValidationStep.ContainsPunctuation |> ValidationStep.Not |> validate

    let containsWhiteSpace = ValidationStep.ContainsWhiteSpace |> validate

    let doesNotContainWhiteSpace =
        ValidationStep.ContainsWhiteSpace |> ValidationStep.Not |> validate

    let isLength (minimum: int) (maximum: int) =
        ValidationStep.Length(Some minimum, Some maximum) |> validate

    let isMinimumLength (minimum: int) =
        ValidationStep.Length(Some minimum, None) |> validate

    let isMaximumLength (maximum: int) =
        ValidationStep.Length(None, Some maximum) |> validate

    let isRegexMatch (pattern: string) =
        ValidationStep.RegexMatch pattern |> validate

    let isNotRegexMatch (pattern: string) =
        ValidationStep.RegexMatch pattern |> ValidationStep.Not |> validate
