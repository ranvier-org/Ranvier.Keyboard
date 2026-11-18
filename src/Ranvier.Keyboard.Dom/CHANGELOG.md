---
include:
  - ../Fable.Ranvier.Keyboard.Dom/
  - ../../Directory.Build.props
depends_on:
  - ../Ranvier.Keyboard/
updaters:
  - xml:
      file: Ranvier.Keyboard.Dom.fsproj
      selector: /Project/PropertyGroup/Version
  - xml:
      file: ../Fable.Ranvier.Keyboard.Dom/Fable.Ranvier.Keyboard.Dom.fsproj
      selector: /Project/PropertyGroup/Version
---

# Changelog

This changelog is generated from conventional commits by [EasyBuild.ShipIt](https://github.com/easybuild-org/EasyBuild.ShipIt).
