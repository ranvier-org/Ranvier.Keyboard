---
last_commit_released: f4b1b41a4dec908b1a5a3c7825e1b603832fbe8b
include:
  - ../Ranvier.Keyboard/
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

## 0.2.0 - 2026-11-19

### 🚀 Features

* *(keyboard)* Build typed chords with inline modifier operations ([68e3658](https://github.com/ranvier-org/Ranvier.Keyboard/commit/68e3658456de4867f36611c27ec6564d6e1c85a9))

### 🔗 Dependencies

* `src/Ranvier.Keyboard` updated to 0.2.0

<strong><small>[View changes on Github](https://github.com/ranvier-org/Ranvier.Keyboard/compare/81101caf84001b2a6d5115f3f49d405d0cbf7c72..f4b1b41a4dec908b1a5a3c7825e1b603832fbe8b)</small></strong>

## 0.1.0 - 2026-11-18

### 🚀 Features

* Initialise Ranvier.Keyboard ([81101ca](https://github.com/ranvier-org/Ranvier.Keyboard/commit/81101caf84001b2a6d5115f3f49d405d0cbf7c72))

### 🔗 Dependencies

* `src/Ranvier.Keyboard` updated to 0.1.0
