﻿module HtmlCache

open System
open System.IO
open System.Net.Http
open FSharp.Data


let cacheFolder = @"../../../cache"
let sep = string Path.DirectorySeparatorChar


let private downloadUrl (uri: Uri) =
  async {
    Console.WriteLine(sprintf "Downloading %O" uri)
    use client = new HttpClient()
    let! html = client.GetStringAsync(uri) |> Async.AwaitTask
    let pathSegments = uri.AbsolutePath.Split("/", StringSplitOptions.RemoveEmptyEntries)
    let path = pathSegments.[0 .. pathSegments.Length - 2] |> String.concat sep
    let name = pathSegments |> Array.last
    let filename = cacheFolder + sep + path + sep + name + ".html"
    filename |> Path.GetDirectoryName |> Directory.CreateDirectory |> ignore
    do! File.WriteAllTextAsync(filename, html, Text.Encoding.UTF8) |> Async.AwaitTask
  }


let refresh =
  async {
    let baseUrl = "https://v4.mui.com"

    Directory.Delete(cacheFolder, true)

    let! testApiPage = ComponentApiPage.AsyncLoad(baseUrl + "/api/app-bar")

    let apiPageUrls =
      testApiPage.Html.CssSelect("nav ul li").[2].CssSelect("ul li")
      |> List.choose (fun menuItem ->
        let links = menuItem.CssSelect("a")
        if List.isEmpty links then None
        else
          let relUrl = links.[0].AttributeValue("href")
          baseUrl + relUrl |> Uri |> Some
      )

    let otherUrls =
      ["/guides/localization"]
      |> List.map (sprintf "%s%s" baseUrl >> Uri)

    do!
      apiPageUrls @ otherUrls
      |> List.map downloadUrl
      |> Async.Parallel
      |> Async.Ignore
  }


let getApiFiles () =
  Directory.GetFiles(cacheFolder + string Path.DirectorySeparatorChar + "api", "*.html")
  |> Array.sortBy (fun s -> s.Substring(0, s.LastIndexOf ".").Replace("-", ""))

let localizationFile = Path.Combine(cacheFolder, "guides", "localization.html")
