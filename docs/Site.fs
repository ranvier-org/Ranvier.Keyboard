module Docs.Site

open Feliz.ViewEngine
open Nacara.Core
open Nacara.Plugins
open Nacara.Theme

let versions = [ SiteVersion.root "0.1" ]

let theme =
    Theme.defaults
    |> Theme.navbar [ NavbarSection ("Guide", "guide", "/guide/introduction/") ]
    |> Theme.navbarEnd
        [
            NavbarDynamicWidget (Versions.switcher (Versions.versions versions Versions.defaults))
            NavbarIcon ("GitHub", "https://github.com/ranvier-org/Ranvier.Keyboard", Icons.github)
        ]
    |> Theme.editUrl "https://github.com/ranvier-org/Ranvier.Keyboard/edit/main/docs"
    |> Theme.footer (Html.p [ Html.text "Built with Nacara" ])

let site =
    Site.create "Ranvier.Keyboard"
    |> Site.baseUrl "/Ranvier.Keyboard/"
    |> Site.origin "https://ranvier-org.github.io"
    |> Site.output "output"
    |> Markdown.register
    |> TreeSitter.register
    |> Literate.register
    |> Sitemap.register
    |> LinkValidator.registerWith (LinkValidator.checkExternal false)
    |> LightningCss.register
    |> Esbuild.register
    |> Nuglify.minifyHtml
    |> Versions.register versions
    |> GitHubPages.register
    |> Theme.register theme
    |> Site.collection (Theme.docs theme "content")

[<EntryPoint>]
let main argv =
    Nacara.run site argv
