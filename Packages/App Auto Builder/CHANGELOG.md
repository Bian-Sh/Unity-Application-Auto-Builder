# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).


## [2.2.0] - 2024-09-25

### New Feature

* FunctionTask 参数会自动转换
* 支持带授权的（带产品ID） Virbox 加密
* 实现了文件/文件夹的拷贝、删除、重命名、移动

### Fixed

* 解决非本插件触发的打包也会触发插件 Task 的异常



## [2.1.0] - 2024-09-25

### New Feature

* 新增通过控制台超链接定位文件和文件夹的功能，方便查看任务输出成果

## [2.0.1] - 2024-09-24

### Fixed

* 修复了 .nsi 文件抛出 No Directory 的异常
* 修复了 SettingProviders 有些时候会报null 的问题

## [2.0.0] - 2024-09-22

### New Features

* 新增 Virbox Task，支持使用 Virbox 对 exe 输出进行加密（Mono）
* 新增 Nsis Tas，支持使用 Nsis 将 exe 输出包打包成 Windows 安装包
* 新增 SettingProvider，支持在 Unity ProjectSettings 中配置 Virbox 和 Nsis 的路径
* SettingProvider 实现通过 Presets 配置多个 Virbox 和 Nsis 的路径，随心切换

### Enhancements

* 完善了 Task 的行为，现在支持 Task 预校验
* 支持为 Task 传递参数
* 支持 Task 返回值并传递给下一个 Task
* Task 的测试支持传递参数

## [1.3.0] - 2023-10-16

### Changed

* 将工程内部的打包路径存为相对路径，方便工程迁移，多人协作
* 将打包平台的选择内置到 BuildProfiles 中，这样就可以一次打多个平台的 app 了

## [1.2.0] - 2023-09-17

### Enhancements

* 将是否需要 build 的状态展示在 Foldout 右侧，更加直观，折叠也不打紧
* 用户自定义任务新增 Properties 按钮，方便用户定位、展示 Task 资产方便配置参数

## [1.1.0] - 2023-09-16

### Fixed

修复了打安卓输出 .exe 文件的异常

### Enhancements

重绘了 inspector，更友好

### New

支持了 run process task

## [1.0.0] - 2023-05-14

* 首次发布

### Changed

### Breaking Changes

### New Features

### Bug Fixes

### Added

### Fixed

### Removed
