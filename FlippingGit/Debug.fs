module FlippingGit.Debug
    
    let debug = true

    let printfn fmt = 
        if debug then printfn fmt 
        else fprintfn null  fmt
