可以到飞书查看原始格式的文档 <https://aw8x3hgudkx.feishu.cn/wiki/BZHfwR8Pkin6BQkk3VacTZMhnV1?from=from_copylink>
# 概要

对于项目轻量塔防的实现，具体项目目标的实现对象有四类模块，分别是游戏对象，各类管理器，可配置SO以及UI。

以上四种主要对象之间的交互使得轻量塔防项目得以实现。

使用版本2022.3.61f3

# 主要管理器

各类管理器在本项目里为众多的游戏对象提供包括运行时管理，模块间，对象间多对多通信或各类业务逻辑等内容。

## 事件总线

该类支持各种类订阅或发布任何实现了IEvent的结构体或类，发布事件是将实现了IEvent的对象作为参数传递给事件总线，事件总线遍历在此处订阅了对应的IEvent类型的对象并分发给对应的订阅事件并调用他们，支持优先度（Early，Default，Late的可扩展的枚举）触发。

## 对象池

因为项目有关于性能的需求，考虑到计划的对象数量相当多，故此处采用对象池优化避免同一时间生成大量对象导致卡顿。

能够被对象池管理的对象要实现接口IPoolable，接口定义了对象池管理对象在特定时间节点的生命周期操作。

对象池读取设置的对象池设置，利用反射搜索内部的相关实现了Poolable的脚本对象，并将其作为主要索引和如果来实例化和管理。在向对象池获取对象时，如果无多余对象会自动扩容。、

## 建筑管理器

如果有建筑管理器拥有建筑数据，在按下鼠标左键时会进行射线检测，如果命中一个合法的可建造的塔，则发布建造事件

## 单位管理器

在运行状态下的敌人会在此注册，以便于UI增减以及索敌等需要遍历的情况。

## 输入管理

输入管理封装输入，将输入内容封装为事件并在相关鼠标操作时进行发布

## 资产管理器

这个类允许通过字符串搜索并获取在此处手动注册的对象，以避免潜在的多次反射开销，使得纯json配置成为可能。

## 波次管理器

在特定时间调用敌人生成器来生成敌人，数据由波次数据驱动，每一波次开始时都会开一个协程来实现生成任务。

## 消费管理器（经济管理）

接受建造塔的扣费购买临时buff和击杀敌人的加费，并存储当前金币数量。

# 主要对象及组件

## 对象中心类（某某某Main）

这些类继承自MonoBehavior，他独立管理内部对象的生命周期，接受各类对象的查询并返回相关引用。

通过中心类避免多余的MonoBehavior静态和动态开销（主要是静态，并更容易的控制顺序），每个组件类只需要实现对应的受管理接口（主要都是生命周期）即可。

## Buff管理类（BuffManager）

Buff管理类接受外部的buff增删改查，并且处理buff之间的冲突或者重复情况，拥有的是若干个buffInstance，buffInstance旨在管理各种各样buff的运行时逻辑。

## Buff

塔，子弹，敌人都拥有buff管理类

有关于buff实现，Buff章节将会进一步解释。

## 塔组件类

### 子弹发射器

内置一个计时器，计时器归零时会调用索敌尝试获取一个离保护点最近的敌人，如有敌人，向对象池申请生成一个子弹并发送设置子弹数据的事件

### 索敌

从单位管理器遍历所有敌人的位置，筛选在攻击范围内的敌人对象，随后读他们移动管理器内距离路径最终点（也就是保护点）的距离，以最近距离的对象为目标返回。

### 建造器

应用塔数据，将数值提供给塔的各个组件。

## 敌人组件类

### 攻击接受类

接受命中敌人的事件，随后添加相关的buff，计算是否暴击，造成伤害。

### 生命值处理

接受造成的伤害，优先扣除护甲值，随后才扣除生命值，生命值小于等于0时调用死亡方法，回收时由敌人中心类给经济系统添加金钱。

### 敌人移动类

基于NavMeshAgent实现的移动类，接受路径设置，可以停止开始移动，计算到达目标的距离等。

### 数据读取类

类似于建造器，应用敌人数据，将数值提供给敌人的各个组件。

## 子弹组件类

### 追踪移动类

追踪Target的敌人，直到距离足够近，便沿着当前方向继续前进。

### 攻击检查类

命中到任何对象时调用其检查方法，并且记录碰撞对象避免重复。可以接受来自于数据的攻击策略来进行配置。

具体的攻击策略可单独实现，实现接口即可，主要在处理碰撞时调用。

## UI

### 敌人血量UI以及管理器

敌人在向UnitManager注册时，响应上述事件，会分配对应的UI绑定对应敌人的相关变化事件以及Transform，使得UI和敌人分离的同时可以最终并响应敌人血量的修改。

血量UI是使用一个Slider改的，可以显示血量和敌人的信息。

### 建造面板

会从AssetManager获取所有注册的Tower，将他们的数据交付给BuildItem显示。

BuildItem有按钮组件，按下后向建筑管理器设置要建造的塔数据以准备进行建造。

### 升级面板

点击合法建筑后出现，可以升级（当前塔等级加一）和拆除（恢复原始塔）

### 塔信息面板

鼠标移动到塔上显示，显示塔名称，等级，过往dps以及预计区间最大dps

### 波次时间线

通过敌人生成管理器计算出每一波次敌人的开始事件，持续时间，并将其转换为时间线

### 时间流速控制

默认情况下调整时间流速可以从0.5x到3x，通过直接修改TimeScale实现，按下按钮可以恢复（默认情况下是1x）

# 可配置内容

## Buff

Buff是分层实现的，从上到下不断从运行时逻辑到SO的持久性逻辑

### Buff实例

buff的运行时存储与处理逻辑，携带一个buff。

接受无效化，移除，计算剩余时间层数以及周期性调用等依赖运行时字段与逻辑，并且通过传自己进入BuffEffect中使得buff可以处理一部分的运行时逻辑。

### Buff

多个buff效果的容器，并且明确层数，持续时间等上层buff示例初始化本身的信息。

可以设置重复buff和冲突buff的具体对象的策略枚举。

### Buff效果

细化的buff逻辑，可以在触发时，移除时，特定事件发布时，以及周期性调用四种情况下提供对应方法内逻辑，上述方法都会传递buffInstance以便处理各种运行时状态。

## 主要数据以及配置示例

### 路径数据

敌人数据由若干个检查点构成，最后一个检查点默认为保护点（作为只读静态字段存储）

具备编辑器，通过射线检测可以在Scene内快速标记路径点并应用。

{

&nbsp; "wayPoints": \[

&nbsp;   {

&nbsp;     "x": 13.0235891,

&nbsp;     "y": -0.210000873,

&nbsp;     "z": 15.75312

&nbsp;   },

&nbsp;   {

&nbsp;     "x": 15.098011,

&nbsp;     "y": -0.21000123,

&nbsp;     "z": 3.79532051

&nbsp;   },

&nbsp;   {

&nbsp;     "x": 7.66304159,

&nbsp;     "y": -0.21000284,

&nbsp;     "z": -5.007536

&nbsp;   },

&nbsp;   {

&nbsp;     "x": -1.47105026,

&nbsp;     "y": -0.209999725,

&nbsp;     "z": -10.128232

&nbsp;   },

&nbsp;   {

&nbsp;     "x": -6.42793369,

&nbsp;     "y": -0.210001,

&nbsp;     "z": -8.433941

&nbsp;   },

&nbsp;   {

&nbsp;     "x": -19.0,

&nbsp;     "y": 0.25,

&nbsp;     "z": 2.5

&nbsp;   }

&nbsp; ]

}

\[图片]

### 波次数据

波次数据由波数据，敌人数据构成，游戏内总共有若干波，每一波有若干敌人数据，敌人数据可以配置敌人种类，个数，间隔时间，相比于波次开始时的生成延迟等。

\[图片]

{

&nbsp; "waveData": \[

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Slime.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/UpLeftRoad.asset",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 100,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 30

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Slime.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/UpLeftRoad.asset",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 20,

&nbsp;         "spawnInterval": 0.5,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "Enemy/Default",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/UpLeftRoad.asset",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 5,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 5.0

&nbsp;       },

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Slime.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightDirectly.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 10,

&nbsp;         "spawnInterval": 0.5,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Slime.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightDirectly.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 10,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 5.0

&nbsp;       },

&nbsp;       {

&nbsp;         "enemyDataReference": "Enemy/Default",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightDirectly.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 10,

&nbsp;         "spawnInterval": 2.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Orc.asset",

&nbsp;         "wayPointsDataReference": "WayPoints/Default",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 10,

&nbsp;         "spawnInterval": 2.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Witch.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightDirectly.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 10,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       },

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Goblin.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/UpLeftRoad.asset",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 30,

&nbsp;         "spawnInterval": 0.75,

&nbsp;         "spawnDelay": 5.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Gargoyle.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightUpRoad.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 1,

&nbsp;         "spawnInterval": 2.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       },

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Gargoyle.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/UpLeftRoad.asset",

&nbsp;         "spawnerIndex": 0,

&nbsp;         "spawnCount": 1,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 5.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   },

&nbsp;   {

&nbsp;     "spawnData": \[

&nbsp;       {

&nbsp;         "enemyDataReference": "path:Assets/Scripts/Data/Enemy/EnemyData/Fast Runner.asset",

&nbsp;         "wayPointsDataReference": "path:Assets/Scripts/Data/Enemy/WayPoints/RightUpRoad.asset",

&nbsp;         "spawnerIndex": 1,

&nbsp;         "spawnCount": 1,

&nbsp;         "spawnInterval": 1.0,

&nbsp;         "spawnDelay": 0.0

&nbsp;       }

&nbsp;     ],

&nbsp;     "spawnDelay": 15

&nbsp;   }

&nbsp; ],

&nbsp; "waitTimeBeforeStart": 0

}

### 塔数据

塔数据由多个等级数据构成，等级数据内包括名字，费用，攻击，初始buff等基本数据等

\[图片]

{

&nbsp; "levelData": \[

&nbsp;   {

&nbsp;     "name": "Arrow",

&nbsp;     "cost": 100,

&nbsp;     "basicAttack": 50.0,

&nbsp;     "attackRange": 10.0,

&nbsp;     "basicAttackCoolDown": 2.0,

&nbsp;     "criticalRate": 0.1,

&nbsp;     "damageType": "Normal",

&nbsp;     "towerViewPrefabReference": "path:Assets/Art/viewPrefabs/Towers/ArrowTower\_0.prefab",

&nbsp;     "buffReferences": \[]

&nbsp;   },

&nbsp;   {

&nbsp;     "name": "Arrow",

&nbsp;     "cost": 140,

&nbsp;     "basicAttack": 60.0,

&nbsp;     "attackRange": 15.0,

&nbsp;     "basicAttackCoolDown": 2.0,

&nbsp;     "criticalRate": 0.2,

&nbsp;     "damageType": "Normal",

&nbsp;     "towerViewPrefabReference": "path:Assets/Art/viewPrefabs/Towers/ArrowTower\_1.prefab",

&nbsp;     "buffReferences": \[

&nbsp;       "Buff/Penetrate"

&nbsp;     ]

&nbsp;   },

&nbsp;   {

&nbsp;     "name": "Arrow",

&nbsp;     "cost": 180,

&nbsp;     "basicAttack": 90.0,

&nbsp;     "attackRange": 18.0,

&nbsp;     "basicAttackCoolDown": 1.5,

&nbsp;     "criticalRate": 0.2,

&nbsp;     "damageType": "Normal",

&nbsp;     "towerViewPrefabReference": "path:Assets/Art/viewPrefabs/Towers/ArrowTower\_2.prefab",

&nbsp;     "buffReferences": \[

&nbsp;       "Buff/Penetrate",

&nbsp;       "Buff/CarryAddCrit"

&nbsp;     ]

&nbsp;   }

&nbsp; ]

}

### Buff数据

buff数据内填写负数值代表不启动，可以配置持续时间，层数，最大层数，优先级（冲突处理的策略之一），buff效果，冲突的buff以及处理策略，相同策略处理逻辑

\[图片]

{

&nbsp; "duration": 30.0,

&nbsp; "stacks": -1,

&nbsp; "maxStacks": -1,

&nbsp; "tickInterval": 0.0,

&nbsp; "effectReferences": \[

&nbsp;   "path:Assets/Scripts/Buff/buffEffects/Data/AddTowerShootSpeedEffect.asset"

&nbsp; ],

&nbsp; "conflictConfigs": \[],

&nbsp; "sameStrategy": 2

}



