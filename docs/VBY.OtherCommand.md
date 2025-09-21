# VBY.OtherCommand 辅助命令

- 作者: xuyuwtu
- 出处: [MyPlugin](https://github.com/xuyuwtu/MyPlugin)
- 一些神秘的辅助命令


## 指令

| 命令 | 权限 | 说明 |
| --- |:-: | :-: |
| /spawnmobply(smp) <玩家> <生物> [数量]  | tshock.npc.spawnmob | 生成一定数量的NPC到玩家附近 |
| /ouser | tshock.superadmin.user | 用户管理 |
| /paintworld item <物品id> | other.admin | 给整个世界涂漆(油漆物品) |
| /paintworld id <油漆id/random> | other.admin | 给整个世界涂漆(油漆id)  |
| /loadworld <文件名> | other.admin | 切换世界 |
| /spawncultistritual | other.admin | 生成拜月教祭祀活动，支持跳过检(-s)和显示生成位置信息(-i) |
| /getobjinfo | other.admin | 获取对象信息, |
| /itemquery chest| other.admin | 查找游戏内箱子物品 |
| /itemquery player| other.admin | 查找游戏内玩家物品 |
| /clearworld empty | other.admin | 清空整个世界 |
| /clearworld airisland | other.admin | 将整个世界清理为抽象空岛 |
| /conwho | tshock.admin.kick | 列出已连接的用户 |
| /conkick | tshock.admin.kick | 踢出连接的用户 |
| /warp | - | 管理、使用传送点 |

### `/itemquery` 命令表格

| 参数类别 | 参数       | 说明                                                                 | 示例                                |
|:-:|:-:|--------------------------------------------------------------------------|-----------------------------------------|
| 模式     | `chest`        | 查询箱子中的物品。                                                       | `/itemquery chest type == 1`            |
|              | `player`       | 查询玩家物品栏中的物品。                                                 | `/itemquery player stack > 50`          |
| 属性     | `type`         | 物品的 ID。                                                              | `/itemquery chest type == 1`            |
|              | `stack`        | 物品的堆叠数量。                                                         | `/itemquery player stack > 50`          |
|              | `Name`         | 物品的名称（字符串，需用引号包裹）。                                      | `/itemquery player Name == "Gold Coin"` |
|              | 其他属性       | 支持 `Item` 类的所有公共属性（如 `prefix`、`favorited` 等）。             | `/itemquery chest prefix == 10`         |
| 运算符   | `==`           | 等于。                                                                   | `/itemquery chest type == 1`            |
|              | `!=`           | 不等于。                                                                 | `/itemquery chest type != 1`            |
|              | `>`            | 大于。                                                                   | `/itemquery player stack > 50`          |
|              | `<`            | 小于。                                                                   | `/itemquery player stack < 100`         |
|              | `>=`           | 大于等于。                                                               | `/itemquery chest stack >= 10`          |
|              | `<=`           | 小于等于。                                                               | `/itemquery chest stack <= 99`          |
| 值       | 整数           | 用于比较 `type` 或 `stack` 等数值属性。                                   | `/itemquery chest type == 1`            |
|              | 字符串         | 用于比较 `Name` 等字符串属性（需用引号包裹）。                            | `/itemquery player Name == "Gold Coin"` |


## 配置
```json5
无
```
## 更新日志

```
无
```

## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love
