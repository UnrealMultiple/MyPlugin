# VBY.PluginLoader VBY插件加载器

- 作者: xuyuwtu
- 出处: [MyPlugin](https://github.com/xuyuwtu/MyPlugin)
- 可以热重载、热加载、热卸载的插件加载器



## 指令

| 命令 |  权限  |  说明 |
| :-: | :-: | :-: |
| /load load | vby.pluginloader | 加载所有PluginLoader所管理的插件 |
| /load unload| vby.pluginloader | 卸载PluginLoader所管理的插件 |
| /load reload| vby.pluginloader | 重载PluginLoader所管理的插件 |
| /load plugins| vby.pluginloader | 显示PluginLoader已加载的插件 |
| /load info | vby.pluginloader | 显示PluginLoader调试信息 |
| /load clear | vby.pluginloader | 清理旧Loader并且重置LoaderCount |

## 配置
> 配置文件位置：Config/VBY.PluginLoader.json
```json5
{
  "loadFromDefault": [
    "System.Data.Common",
    "TerrariaServer",
    "TShockAPI",
    "OTAPI",
    "OTAPI.Runtime",
    "MySql.Data",
    "Microsoft.Data.Sqlite",
    "MonoMod.RuntimeDetour",
    "ModFramework",
    "VBY.PluginLoader"
  ],
  "loadFiles": [
    "PluginLoader/*.dll"
  ]
}
```
## 更新日志

```
无
```

## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love
