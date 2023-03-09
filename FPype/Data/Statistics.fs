namespace FPype.Data

module Statistics =
    
    
    
    let sum (values: decimal list) = values |> List.sum
    
    let max (values: decimal list) = values |> List.max


    let count (values: decimal list) = values.Length
    
    let min (values: decimal list) = values |> List.min
    
    let mean (values: decimal list) = (sum values) / (decimal (count values))
    
    let variance (values: decimal list) =
        let m = mean values
        
        values
        |> List.map (fun v -> pown (v - m) 2)
        |> List.sum
        // NOTE - check this is correct
        |> fun r -> r / decimal (count values)

    let standardDeviation (values: decimal list) =
        float (variance values) |> sqrt 