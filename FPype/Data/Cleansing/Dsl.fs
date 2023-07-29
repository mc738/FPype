module FPype.Data.Cleansing

open FPype.Data.Cleansing

module Dsl =

    let validate (step: ValidationStep) = CleansingStep.Validate step

    let validation (steps: ValidationStep list) = steps |> List.map validate

    let transform (step: TransformationStep) = CleansingStep.Transform step

    let transformation (steps: TransformationStep list) = steps |> List.map transform
    
    let containsCharacters (chars: char list) = ValidationStep.ContainsCharacters chars |> validate
    
    let contains (str: string) = ValidationStep.Contains str |> validate
    
    let containsLetters = ValidationStep.ContainsLetters |> validate
    
    let containsNumbers = ValidationStep.ContainsNumbers |> validate
    
    
