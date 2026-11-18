---
last_commit_released: 81101caf84001b2a6d5115f3f49d405d0cbf7c72
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

## 0.1.0 - 2026-11-18

### 🚀 Features

* Initialise Ranvier.Keyboard ([81101ca](https://github.com/ranvier-org/Ranvier.Keyboard/commit/81101caf84001b2a6d5115f3f49d405d0cbf7c72))

### 🔗 Dependencies

* `src/Ranvier.Keyboard` updated to 0.1.0
